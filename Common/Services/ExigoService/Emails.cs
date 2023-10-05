using Common;
using Common.Api.ExigoWebService;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ExigoService
{
    public static partial class ExigoDAL
    {
        public static void SendEmail(SendEmailRequest request)
        {
            var email = new MailMessage();

            email.From = new MailAddress(request.From);
            email.Subject = request.Subject;

            email.Body = request.Body;
            email.IsBodyHtml = true;

            var SmtpServer = new SmtpClient();
            SmtpServer.Host = GlobalSettings.Emails.SMTPConfigurations.Default.Server;
            SmtpServer.Port = GlobalSettings.Emails.SMTPConfigurations.Default.Port;
            SmtpServer.Credentials = new System.Net.NetworkCredential(GlobalSettings.Emails.SMTPConfigurations.Default.Username, GlobalSettings.Emails.SMTPConfigurations.Default.Password);
            SmtpServer.EnableSsl = GlobalSettings.Emails.SMTPConfigurations.Default.EnableSSL;


            // Send the emails
            var tasks = new List<Task>();
            foreach (var address in request.To)
            {
                email.To.Add(new MailAddress(address));
            }


            try
            {
                SmtpServer.Send(email);

            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

        }
    }
}