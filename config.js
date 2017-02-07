module.exports = {
    token : "YOUR-TOKEN-HERE",
    greetings : ['hello', 'hi','hoy', 'ui', 'oi', 'ei','hey','bot','yoh'], // how you want to greet your bot
    greetreplies : ['good morning', 'heya', 'back at ya'], // how you want the bot to greet you
    getByType : ['show me the (.*)', 'what are those (.*)', 'any (.*)'], // param is email type : Normal,Urgent,Meeting,Incident,SR
    getByTypeAndDate : ['are there (.*) (.*)','do i have (.*) on (.*)', '(.*) on (.*) (pls|plz|please)'], // first is email type, second is date keyword.
    negativeReplies : ['none', 'nada', 'wala', "zip", "zilch","zero"] // how bot replies negatively
    // rest omitted for brevity
}