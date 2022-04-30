using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BulkyBook.Utility;

public class EmailSender: IEmailSender
{
    public string SendGridSecret { get; set; }

    public EmailSender(IConfiguration _configuration)
    {
        SendGridSecret = _configuration["SendGrid:SecretKey"];
    }
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        /*
        var emailToSend = new MimeMessage();
        emailToSend.From.Add(MailboxAddress.Parse("abdul.badru@uem.ac.mz"));
        emailToSend.To.Add(MailboxAddress.Parse(email));
        emailToSend.Subject = subject;
        emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html) {Text = htmlMessage};
        
        //send email
        using (var emailClient = new MailKit.Net.Smtp.SmtpClient())
        {
            emailClient.Connect("smpt.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            emailClient.Authenticate("abdul.badru@xpntek.com", "1nsp1r0n@P");
            emailClient.Send(emailToSend);
            
            emailClient.Disconnect(true);
        }
                return Task.CompletedTask;

        */

        var client = new SendGridClient(SendGridSecret);
        var from = new EmailAddress("abdul.badru@uem.ac.mz", "Bulky Book");
        var to = new EmailAddress(email);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);
        return client.SendEmailAsync(msg);




    }
}