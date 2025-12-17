using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Identity.UI.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");

        // Retrieve settings securely from configuration
        var smtpServer = emailSettings["SmtpServer"];
        var port = int.Parse(emailSettings["Port"] ?? "587");
        var senderEmail = emailSettings["SenderEmail"];
        var appPassword = emailSettings["AppPassword"];

        try
        {
            using (var client = new SmtpClient(smtpServer, port))
            {
                client.EnableSsl = true;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(senderEmail, appPassword);

                var mailMessage = new MailMessage(senderEmail, email, subject, message);
                mailMessage.IsBodyHtml = true;

                await client.SendMailAsync(mailMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email to {email}: {ex.Message}");
            throw;
        }
    }
}