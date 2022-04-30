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
    

        var client = new SendGridClient(SendGridSecret);
        var from = new EmailAddress("email@email.mz", "Bulky Book");
        var to = new EmailAddress(email);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);
        return client.SendEmailAsync(msg);




    }
}