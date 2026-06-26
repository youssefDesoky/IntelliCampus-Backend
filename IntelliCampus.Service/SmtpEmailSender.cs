using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace IntelliCampus.Service;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var fullHtml = $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="UTF-8"></head>
        <body style="margin:0;padding:0;background:#f4f6f9;font-family:Arial,sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6f9;padding:20px 0;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);">
                            <tr>
                                <td style="background:#1a56db;padding:24px 32px;text-align:center;">
                                    <h1 style="color:#ffffff;margin:0;font-size:24px;">IntelliCampus</h1>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:32px;">
                                    {htmlBody}
                                </td>
                            </tr>
                            <tr>
                                <td style="background:#f9fafb;padding:16px 32px;text-align:center;font-size:12px;color:#9ca3af;border-top:1px solid #e5e7eb;">
                                    IntelliCampus &mdash; Academic Management System
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;

        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogWarning("SMTP not configured. Would send email to {Email} with subject {Subject}: {Body}",
                toEmail, subject, htmlBody);
            Console.WriteLine($"===== EMAIL TO: {toEmail} =====");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine("Body:");
            Console.WriteLine(fullHtml);
            Console.WriteLine("===================================");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = fullHtml };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {Email} with subject {Subject}", toEmail, subject);
    }
}
