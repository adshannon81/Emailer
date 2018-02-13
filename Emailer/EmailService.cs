using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Emailer
{

    public class EmailService
    {
        public EmailResponse SendEmail(string user, string password, string fromEmail, string[] emailTo, string[] ccTo, string subject, string body, bool isBodyHtml)
        {
            return SendEmailWithAttachments(user, password, fromEmail, emailTo, ccTo, subject, body, isBodyHtml, new string[] { }, "", "");
        }

        public EmailResponse SendEmail(string user, string password, string fromEmail, string[] emailTo, string[] ccTo, string subject, string body, bool isBodyHtml, string logSource, string logName)
        {
            return SendEmailWithAttachments(user, password, fromEmail, emailTo, ccTo, subject, body, isBodyHtml, new string[] { }, logSource, logName);
        }

        public EmailResponse SendEmailWithAttachments(string user, string password, string fromEmail, string[] emailTo, string[] ccTo,
            string subject, string body, bool isBodyHtml, string[] attachments,
            string logSource = "", string logName = "")
        {
            FileAttachment[] fileAttachments = new FileAttachment[attachments.Count()];
            for (int i = 0; i > attachments.Count(); i++)
            {
                fileAttachments[i].FileName = new FileInfo(attachments[i]).Name;
                fileAttachments[i].FileContentBase64 = Convert.ToBase64String(File.ReadAllBytes(attachments[i]));
            }

            return SendEmailWithAttachments(user, password, fromEmail, emailTo, ccTo, subject, body, isBodyHtml, fileAttachments, logSource, logName);
        }

        public EmailResponse SendEmailWithAttachments(string user, string password, string fromEmail, string[] emailTo, string[] ccTo,
            string subject, string body, bool isBodyHtml, FileAttachment[] attachments,
            string logSource = "", string logName = "")
        {
            bool isHtml = isBodyHtml;

            if (logSource == "")
            {
                try
                {
                    logSource = ConfigurationManager.AppSettings["LogSource"].ToString();
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to read LogSource from app settings", ex);
                }
            }
            if (logName == "")
            {

                try
                {
                    logName = ConfigurationManager.AppSettings["logName"].ToString();
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to read LogSource from app settings", ex);
                }
            }

            EventLogger eventLogger = new EventLogger(logName, logSource);
            EmailResponse emailResponse = new EmailResponse();

            string mailDetails = "Email Service:" + Environment.NewLine +
                            "From: " + user + Environment.NewLine +
                            "To: " + string.Join("", emailTo) + Environment.NewLine;
            if (ccTo != null)
            {
                mailDetails += "CC To: " + string.Join("", ccTo) + Environment.NewLine;
            }
            mailDetails += "Subject: " + subject + Environment.NewLine +
                    "IsBodyHtml: " + isHtml + Environment.NewLine +
                    "body: " + body;

            if (ConfigurationManager.AppSettings["DeployedInTestEnvironment"].ToString() == "true")
            {
                bool error = false;
                try
                {
                    eventLogger.WriteEntry("Email Service Successfully replicated: " + Environment.NewLine
                    + mailDetails, EventLogEntryType.Information);

                }
                catch //(Exception ex)
                {
                    error = true;
                    //throw new Exception("Unable to find LogName/LogSource on Machine; " + logName + ", " + logSource + ", " + Environment.MachineName, ex);
                }

                if (error)
                {
                    try
                    {
                        EventLog.CreateEventSource(logSource, logName);
                        eventLogger.WriteEntry("Email Service Successfully replicated: " + Environment.NewLine
                        + mailDetails, EventLogEntryType.Information);

                    }
                    catch (Exception ex)
                    {
                        eventLogger = new EventLogger("Application", "Application Error");
                        eventLogger.WriteEntry("Unable to find LogName/LogSource on Machine and could not create the source automatically; "
                            + logName + ", " + logSource + ", " + Environment.MachineName + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace,
                            EventLogEntryType.Error);
                        throw new Exception("Unable to find LogName/LogSource on Machine and could not create the source automatically; " + logName + ", " + logSource + ", " + Environment.MachineName, ex);
                    }
                }
            }
            else
            {
                try
                {
                    eventLogger.WriteEntry(mailDetails, EventLogEntryType.Information);
                    emailResponse = new EmailResponse();
                    emailResponse.SuccessfullySent = true;
                    emailResponse.Message = "";

                    SendEmailSMTP(user, password, fromEmail, emailTo, ccTo, subject, body, isHtml, attachments);

                    string successMessage = "From: " + user + Environment.NewLine +
                        "To: " + string.Join("", emailTo) + Environment.NewLine;

                    if (ccTo != null)
                    {
                        successMessage += "CC To: " + string.Join("", ccTo) + Environment.NewLine;
                    }
                    successMessage += "Subject: " + subject + Environment.NewLine +
                            "body: " + body;

                    if (attachments != null && attachments.Count() > 0)
                    {
                        successMessage += Environment.NewLine + "has " + attachments.Count().ToString() + " attachment(s)";
                    }

                    eventLogger.WriteEntry("Email Service Successful: " + Environment.NewLine + successMessage, EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    emailResponse.SuccessfullySent = false;
                    emailResponse.Message = ex.Message;

                    eventLogger.WriteEntry("Email Service Failed: SendEmailSMTP -  " + ex.Message + Environment.NewLine + ex.StackTrace, EventLogEntryType.Error);

                }
            }

            return emailResponse;
        }

        private static bool SendEmailSMTP(string user, string password, string fromEmail, string[] emailTo, string[] ccTo, string subject, string body, bool isBodyHtml, FileAttachment[] attachments)
        //    string subject, string emailBody, bool isError = false)
        {
            bool success = false;

            MailMessage msg = new MailMessage();
            foreach (string emailAddress in emailTo)
            {
                msg.To.Add(new MailAddress(emailAddress));
            }
            msg.From = new MailAddress(fromEmail);
            msg.Subject = subject;
            msg.Body = body;
            msg.IsBodyHtml = true;

            if (attachments != null && attachments.Length > 0)
            {
                foreach (FileAttachment fileAttachment in attachments)
                {
                    byte[] bytes = System.Convert.FromBase64String(fileAttachment.FileContentBase64);
                    MemoryStream memAttachment = new MemoryStream(bytes);
                    Attachment attachment = new Attachment(memAttachment, fileAttachment.FileName);
                    msg.Attachments.Add(attachment);
                }
            }

            var smtpClient = new SmtpClient
            {
                Host = ConfigurationManager.AppSettings["EmailSMTPServer"].ToString(),
                Port = Convert.ToInt16(ConfigurationManager.AppSettings["EmailSMTPPort"].ToString()),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    user,
                    password
                )
            };

            //try
            //{
            smtpClient.Send(msg);
            success = true;
            //}
            //catch (Exception ex)
            //{
            //    success = false;
            //}

            return success;
        }
    }


}
