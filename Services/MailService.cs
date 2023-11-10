using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using ThingsBoardPublisher.Settings;
using Serilog;

namespace ThingsBoardPublisher.Services;

public class MailService
{
    private readonly EmailSetting _emailSetting;

    public MailService(IOptions<EmailSetting> emailSetting)
    {
        _emailSetting = emailSetting.Value;
    }

    public void SendMail(string subject, string body)
    {
        try
        {
            // create email message
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailSetting.FromEmail));
            email.To.Add(MailboxAddress.Parse(_emailSetting.ToEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            // send email
            using var smtp = new SmtpClient();
            smtp.Connect(_emailSetting.Url, _emailSetting.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_emailSetting.UserName, _emailSetting.Password);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex.FlattenException());
        }
    }
}
