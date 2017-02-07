var Botkit = require('botkit/lib/Botkit.js');
var config = require('./config.js');
var helper = require('./helper.js');
var dtbase = require('./dtbase.js');
dtbase.load();
var controller = Botkit.slackbot({
    debug: true
});

var bot = controller.spawn({
    // token: process.env.token
    token : config.token,
    retry: Infinity
}).startRTM();


controller.hears(config.greetings, 'direct_message,direct_mention,mention', function(bot, message) {

    bot.startConversation(message, function(err, convo) {
        if (!err) {
            convo.say(helper.getOneAtRandom(config.greetreplies));
            convo.ask("ready for the rundown?", [
                {
                    pattern: 'yes',
                    callback: function(response, convo) {
                        // since no further messages are queued after this,
                        // the conversation will end naturally with status == 'completed'
                        convo.next();
                    }
                },
                {
                    pattern: 'no',
                    callback: function(response, convo) {
                        // stop the conversation. this will cause it to end with status == 'stopped'
                        convo.stop();
                    }
                },
                {
                    default: true,
                    callback: function(response, convo) {
                        convo.repeat();
                        convo.next();
                    }
                }
            ]);
            
            convo.on('end', function(convo) {
                if (convo.status == 'completed') {
                    bot.reply(message,"here's the rundown: " + "\n" +  dtbase.getRundown());
                } else {
                    // this happens if the conversation ended prematurely for some reason
                    bot.reply(message, 'OK, nevermind!');
                }
            });
        }   
    });

    
});

controller.hears(config.getByTypeAndDate, 'direct_message,direct_mention,mention', function(bot, message){
    var etype = message.match[1];
    var targetDate = message.match[2];    
    bot.reply(message, dtbase.getByTypeAndDate(etype,targetDate));
});

controller.hears(config.getByType, 'direct_message,direct_mention,mention', function(bot, message){
    var etype = message.match[1];
    
    bot.reply(message, dtbase.getByType(etype));
});


// rest omitted for brevity