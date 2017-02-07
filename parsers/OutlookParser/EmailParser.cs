using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Outlook = Microsoft.Office.Interop.Outlook;
using Office = Microsoft.Office.Core;
using System.IO;
using Newtonsoft.Json;
using System.Configuration;
using System.Diagnostics;

namespace OutlookParser
{
    public partial class ThisAddIn
    {
        private string CALENDARFOLDER = "00000000E798E87342110B4889D675FAB02D15A20100D88F0B47E5A2DB47BFB8B66ECBE181EC000000B5AC240000";
        private string INCIDENTFOLDERID = "00000000E798E87342110B4889D675FAB02D15A20100F5309BDD660504419A520AB909F533EC000B0A4481E50000";

        private Outlook.Items inboxItems;
        private Outlook.Items outlookCalendarItems;
        private Outlook.Items incidentItems;

        private string MainPath; 
        private string SNEmail;
        private List<String> Keywords;
        private List<String> Priorities;
        
        private List<EmailItem> DB;

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            DB = new List<EmailItem>();
            MainPath = ConfigurationManager.AppSettings["MainPath"];
            SNEmail = ConfigurationManager.AppSettings["SNEmail "];
           
            Outlook.MAPIFolder inbox = Application.Session.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderInbox);
            inboxItems = inbox.Items;
            inboxItems.ItemAdd += new Outlook.ItemsEvents_ItemAddEventHandler(InboxFolderItemAdded);

            var calendarFolder = Application.Session.GetFolderFromID(CALENDARFOLDER);
            outlookCalendarItems = calendarFolder.Items;
            outlookCalendarItems.ItemAdd += new Outlook.ItemsEvents_ItemAddEventHandler(outlookCalendarItems_ItemAdd);

            var incidentFolder = Application.Session.GetFolderFromID(INCIDENTFOLDERID);
            incidentItems = incidentFolder.Items;
            incidentItems.ItemAdd += new Outlook.ItemsEvents_ItemAddEventHandler(incidentItems_ItemAdded);

            // Process folders
            ProcessInbox(inbox);
            ProcessMeetings(CALENDARFOLDER);
            

            // Save list to file
            File.WriteAllText(MainPath + "emails.db", JsonConvert.SerializeObject(DB));

        }

        private void ProcessInbox(Outlook.MAPIFolder inbox)
        {
            foreach (var item in inbox.Items)
            {
                if (item is Outlook.MailItem)
                {
                    AddEmailItem((Outlook.MailItem)item);
                }
            }
        }

        private List<string> GetAllKeywords()
        {
            // Simplified the metrics and abstracted all keywords to file
            var keywords = File.ReadAllLines(MainPath + "keywords.db");
            return keywords.ToList();
        }

        private List<string> GetAllPriorities()
        {
            // Simplified the metrics and abstracted all prios to file
            var prios = File.ReadAllLines(MainPath + "priorities.db");
            return prios.ToList();
        }

        private void InboxFolderItemAdded(object Item)
        {
            if (Item is Outlook.MailItem)
            {
                AddEmailItem((Outlook.MailItem)Item);
            }
        }

        private void ProcessMeetings(string CALENDARFOLDER)
        {
            var folder = Application.Session.GetFolderFromID(CALENDARFOLDER);
            if (folder != null)
            {
                // Extract all body
                foreach (Outlook.AppointmentItem item in folder.Items)
                {
                    AddMeetingItem(item);
                }
            }
        }

        private void outlookCalendarItems_ItemAdd(object item)
        {
            AddMeetingItem(item);
        }

        void incidentItems_ItemAdded(object item)
        {
            Outlook.MailItem Item = (Outlook.MailItem)item;
            if (Item.SenderEmailAddress == SNEmail && Item.SentOnBehalfOfName == "ServiceNow Notification Services")
            {
                AddTicketItem(Item);               
            }
        }

        private void AddEmailItem(Outlook.MailItem mailItem)
        {
            if (mailItem is Outlook.MailItem)
            {
                Outlook.MailItem item = (Outlook.MailItem)mailItem;
                if (item.UnRead) // process only new emails
                {
                    if (SubjectContainsKeywords(item.Subject) || 
                        SenderContainsPriorities(item.SenderEmailAddress) || SenderContainsPriorities(item.CC) ||
                        item.Importance == Outlook.OlImportance.olImportanceHigh)
                    {
                        var emailItem = new EmailItem()
                        {
                            Subject = item.Subject,
                            Body = item.Body,
                            DateReceived = item.ReceivedTime,
                            IsUrgent = (item.Importance == Outlook.OlImportance.olImportanceHigh),
                            From = item.SenderName
                        };

                        if (emailItem.IsUrgent) emailItem.EmailType = "Urgent";

                        DB.Add(emailItem);
                    }
                }
            }
        }

        private void AddTicketItem(Outlook.MailItem item)
        {
            var tix = new TicketItem(item);
            var exists = DB.Where(p => ((TicketItem)p).TicketNo == tix.TicketNo).FirstOrDefault();
            if (exists != null)
            {
                if (tix.DateReceived >= exists.DateReceived)
                {
                    ((TicketItem)exists).Status = tix.Status;
                }
            }
            else
            {
                DB.Add(tix);
            }
        }

        private void AddMeetingItem(object item)
        {
            if (item is Outlook.AppointmentItem)
            {
                Outlook.AppointmentItem Item = (Outlook.AppointmentItem)item;
                Meeting mtg = new Meeting()
                {
                    Subject = Item.Subject,
                    Body = Item.Body,
                    DateReceived = Item.CreationTime,

                    StartDate = Item.Start,
                    EndDate = Item.End,
                    Location = String.IsNullOrEmpty(Item.Location) ? "not set" : Item.Location
                };

                DB.Add(mtg);
            }

        }

        private bool SenderContainsPriorities(string sender)
        {
            if (String.IsNullOrEmpty(sender))
                return false;

            Priorities = GetAllPriorities();

            // Flag items that are from priority senders.(or at least CCed)
            bool doesContain = false;

            foreach (var priority in Priorities)
            {
                var senders = sender.ToUpper().Split(';');
                foreach (var snder in senders)
                {
                    if (snder.Contains(priority.ToUpper()))
                    {
                        doesContain = true;
                        break;
                    }
                }

            }
            return doesContain;
        }

        private bool SubjectContainsKeywords(string subject)
        {
            Keywords = GetAllKeywords();

            // Flag items that have certain keywords on the subject.()
            bool doesContain = false;
            foreach (var keyword in Keywords)
            {
                if (subject.ToUpper().Contains(keyword.ToUpper()))
                {
                    doesContain = true;
                    break;
                }
            }                
            return doesContain;
        }

        private string GetEntryID()
        {
            // used to get folder id.    
            var entryID = "";
            var folder = Application.Session.PickFolder();
            if (folder != null)
            {
                entryID = folder.EntryID;
                Debug.WriteLine(folder);
            }
            return entryID;
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
        }


        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }
        
        #endregion

    }
}
