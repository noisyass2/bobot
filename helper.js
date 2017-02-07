var moment = require('moment');

module.exports = {
    getOneAtRandom : function(list)
    {
        var low = 0;
        var high = list.length;
        return list[Math.floor(Math.random() * (high - low) + low)];    
    },
    getDate : function (str) {
       
        var tglogDays = [
            ["lunes", "monday","mon","luns"],
            ["martes","tuesday","tue","tues","mrts"],
            ["miyerkules", "wednesday","wed","myer"],
            ["hwebes","thursday","thu","hweb","hwbes"],
            ["byernes","friday","fri","byer","biyernes","byrns"],
            ["sabado","sbdo","saturday","sat"],
            ["linggo", "sunday","sun", "lnggo","lingo"],
        ];
        var dayFound = -1;

        var todayLiterals = ["today", "ngaun", "kanina", "ngayon", "this day"];
        var tomorrowLiterals = ["bukas", "tomorrow", "bkas"];
        var yesterdayLiterals = ["kahapon", "yesterday", "khpon"];

        for (var i = 0; i < tglogDays.length; i++) {
            var day = tglogDays[i];
            if(day.indexOf(str) > -1) dayFound = i;
        }


        if(tomorrowLiterals.indexOf(str) > -1){
            str = moment().add(1,'days');
        }
        else if (yesterdayLiterals.indexOf(str) > -1) {
            str = moment().add(-1,'days');
        }
        else  if(dayFound > -1)
        {
            str = moment().startOf('isoweek').add(dayFound,'days');
        }
        else if(todayLiterals.indexOf(str) > -1)
        {
            str = moment();
        }else if(this.isNumeric(str))
        {
            str = moment({d:parseInt(str)});
        }
        else {
            str = moment();
        }

        return str;
    },    
    isNumeric : function (n) {
        return !isNaN(parseFloat(n)) && isFinite(n);
    }

}



// rest omitted for brevity