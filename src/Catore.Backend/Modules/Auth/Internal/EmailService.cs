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
        var body = $"We received a request to reset your Catore password.\n\n" +
                   $"Tap the link below to choose a new password:\n{deepLink}\n\n" +
                   $"This link will expire in 1 hour.\n\n" +
                   $"If you didn't request this, you can safely ignore this email — your password won't be changed.\n\n" +
                   $"This is an automated message, please do not reply.";

        await SendEmail(toEmail, "Reset your Catore password", body);
        _logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }

    public async Task SendVerificationEmail(string toEmail, string verifyToken)
    {
        var deepLink = $"catore://verify-email?token={verifyToken}";
        var body = $"Thanks for signing up for Catore!\n\n" +
                   $"Tap the link below to verify your email and activate your account:\n{deepLink}\n\n" +
                   $"This link will expire in 3 minutes. If it expires, you'll need to request a new verification email or sign up again.\n\n" +
                   $"If you didn't create this account, you can safely ignore this email.\n\n" +
                   $"This is an automated message, please do not reply.";

        await SendEmail(toEmail, "Verify your Catore email", body);
        _logger.LogInformation("Verification email sent to {Email}", toEmail);
    }

    private async Task SendEmail(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_config["Email:FromAddress"]!));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

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
    }
}
