using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace BookCommunity.Api.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var host = _config["Email:SmtpHost"] ?? _config["Email__SmtpHost"];
        if (string.IsNullOrWhiteSpace(host)) { _logger.LogWarning("SMTP host not configured; skipping email to {To}", to); return; }
        var port = int.Parse(_config["Email:SmtpPort"] ?? _config["Email__SmtpPort"] ?? "587");
        var username = (_config["Email:SmtpUsername"] ?? _config["Email__SmtpUsername"] ?? "").Trim();
        var password = (_config["Email:SmtpPassword"] ?? _config["Email__SmtpPassword"] ?? "").Replace(" ", "").Trim();
        var senderEmail = _config["Email:SenderEmail"] ?? _config["Email__SenderEmail"] ?? "noreply@pagely.local";
        var senderName = _config["Email:SenderName"] ?? _config["Email__SenderName"] ?? "Pagely";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(username));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        client.AuthenticationMechanisms.Remove("XOAUTH2");
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
