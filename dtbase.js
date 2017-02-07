var moment = require('moment');
var LINQ = require('node-linq').LINQ;
var fs  = require('fs');

var helper = require('./helper.js');
var config = require('./config.js');

module.exports = {
    db : { emails : null },
    load: function () {
        var data = fs.readFileSync('emails.db');
        // var data = fs.readFileSync('mtgs.db');
        this.db.emails = JSON.parse(data);
        // console.log(this.db.emails[0]);
    },
    getRundown : function () {
        var dateToday = moment().dayOfYear();
        var yearToday = moment().year();
        var groupedItems = new LINQ(this.db.emails)
        .Where(function (email) {
            var stDate = moment(email.DateReceived);
            return stDate.dayOfYear() >= dateToday && stDate.year() == yearToday;
        })
        .GroupBy(function(item) { return item.EmailType; });
        
        // var filteredEmails = new LINQ(groupedItems)       
        var results = [];
        for(var key in groupedItems) if(groupedItems.hasOwnProperty(key)) results.push(key + ":" + groupedItems[key].length)
        
        //console.log(results);
        var filteredItems = new LINQ(results)
            .Select(function (item) {
                return item;
            }).ToArray().join('\n');
        return filteredItems;
    },
    getByTypeAndDate : function (etype,targetDate) {
        var target = helper.getDate(targetDate);               
                
        var targetDOY = target.dayOfYear();
        var filteredEmails = new LINQ(this.db.emails)
            .Where(function (email) {
                var stDate = moment(email.DateReceived);            
                if(etype == "Meeting") 
                    stDate = moment(email.StartDate);
                                                        
                return email.EmailType == etype && stDate.dayOfYear() == target.dayOfYear() && stDate.year() == target.year();
            })
            .Select(function (p) {
                var str = "[" + moment(p.DateReceived).format("HH:mm") + "]" +  p.Subject + " - "  + p.From;  
                if(etype == "Meeting")
                    str = p.Subject  + "@" + p.Location + " on " + moment(p.StartDate).format("HH:mm") + " to " +  moment(p.EndDate).format("HH:mm");
                return str;
            }).ToArray().join('\n');
        
        if(!filteredEmails) return helper.getOneAtRandom(config.negativeReplies);
        return  filteredEmails;
    },
    getByType : function (etype) {
        var target ="today";
        return this.getByTypeAndDate(etype,target);
    }
    // rest omitted for brevity
}