using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Catore.Backend.Modules.Auth.Internal;

internal class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetEmail(string toEmail, string resetToken)
    {
        var deepLink = $"catore://reset-password?token={resetToken}";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_config["Email:FromAddress"]!));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Reset your Catore password";
        message.Body = new TextPart("plain")
        {
            Text = $"Tap this link to reset your password: {deepLink}\n\nThis link expires in 1 hour. If you didn't request this, ignore this email."
        };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_config["Email:SmtpHost"]!, int.Parse(_config["Email:SmtpPort"]!), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["Email:SmtpUsername"]!, _config["Email:SmtpPassword"]!);
            await client.SendAsync(message);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }

        _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }
}
