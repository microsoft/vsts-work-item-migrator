using System;
using System.Net;
using System.Net.Mail;
using Common.Config;
using Microsoft.Extensions.Logging;

namespace Logging
{
    public class Emailer
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<Emailer>();

        public Emailer()
        {
        }

        public void SendEmail(ConfigJson configJson, string body)
        {
            bool sendEmailNotification = configJson.SendEmailNotification;
            EmailNotification emailNotification = configJson.EmailNotification;

            // only proceed if sendEmailNotification && emailNotification != null
            if (sendEmailNotification && emailNotification == null)
            {
                throw new Exception("send-email-notification is set to true but there are no email-notification" +
                    " details specified. Please set send-email-notification to false or specify details for" +
                    " email-notification in the configuration-file.");
            }
            else if (!sendEmailNotification || emailNotification == null)
            {
                return;
            }

            SmtpClient client = new SmtpClient(emailNotification.SmtpServer, emailNotification.Port);
            //client.EnableSsl = true; // need to figure this out! Maybe don't need?
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(emailNotification.UserName, emailNotification.Password);
            MailMessage message = new MailMessage();

            MailAddress from = new MailAddress(emailNotification.FromAddress);
            message.From = from;

            if (emailNotification.RecipientAddresses == null || emailNotification.RecipientAddresses.Count == 0)
            {
                throw new Exception("You must specify one or more recipient-addresses under email-notification.");
            }

            foreach (string address in emailNotification.RecipientAddresses)
            {
                message.To.Add(address);
            }

            message.Body = body;
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.Subject = $"WiMigrator Summary at {DateTime.Now.ToString()}";
            message.SubjectEncoding = System.Text.Encoding.UTF8;
            
            try
            {
                client.Send(message);
            }
            catch (SmtpException e)
            {
                Logger.LogError(LogDestination.File, e, "Could not send run summary email because of an issue with the SMTP server." +
                    " Please ensure that the server works and your configuration file input is correct");
            }
            catch (Exception e)
            {
                Logger.LogError(LogDestination.File, e, "Could not send run summary email because of an Exception");
            }

            message.Dispose();
        }
    }
}
