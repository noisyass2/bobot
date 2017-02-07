using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Interop.Outlook;
using System.IO;

namespace OutlookParser
{
    public class EmailItem
    {
        public EmailItem()
        {
            this.EmailType = "Normal";
        }

        public string Subject { get; set; }
        

        public string Body { get; set; }

        public string EmailType { get; set; }

        public bool IsUrgent { get; set; }

        public DateTime DateReceived { get; set; }

        public string From { get; set; }
    }

    public class Meeting : EmailItem
    {

        public Meeting()
        {
            this.EmailType = "Meeting";
        }
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Location { get; set; }

        public string Attendees { get; set; }
    }

    public class TicketItem : EmailItem
    {

        public string TicketNo { get; set; }
        public string Desc { get; set; }
        public string Status { get; set; }
        public string Service { get; set; }

        public TicketItem()
        {
            this.EmailType = "Request";
        }

        public TicketItem(MailItem item)
        {
            //Parse Subject
            var subj = item.Subject.Split(':');
            this.TicketNo = subj[0];
            this.EmailType = this.TicketNo.StartsWith("INC") ? "Incident" : "Request";
            this.Status = subj[1].Contains("assigned") ? "New" : "Done";

            // Parse body each line
            using (StringReader sr = new StringReader(item.Body))
            {
                string line;
                var isTermination = false;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Trim().StartsWith("Description:"))
                    {
                        this.Desc = line.Replace("Description:", "");
                        if (this.Desc.Contains("(MCP) DAILY TERMINATION"))
                        {
                            isTermination = true;
                        }
                        else
                        {
                            isTermination = false;
                        }
                    }
                    if (line.Trim().StartsWith("Service:"))
                    {
                        this.Service = line.Replace("Service:", "");
                    }
                    if (line.Trim().StartsWith("Employee ID #:") && isTermination)
                    {
                        this.Desc += ":" + line.Replace("Employee ID #:", "");
                    }
                }
            }
            if (this.EmailType == "Incident")
            {
                this.IsUrgent = true;
            }
            this.DateReceived = item.ReceivedTime;
        }


    }

    public class Incident : TicketItem
    {
        public Incident()
        {
            this.EmailType = "Incident";
            this.IsUrgent = true;
        }

        
    }


}
