using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MotorShop.Services
{
    // appsettings.json -> "MailSettings": { ... }
    public class MailSettings
    {
        public string Mail { get; set; } = string.Empty;         // account gửi
        public string DisplayName { get; set; } = "MotorShop";
        public string Password { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;         // smtp.gmail.com / smtp-relay...
        public int Port { get; set; } = 587;                     // 587 (STARTTLS) | 465 (SSL)
        public bool UseSsl { get; set; } = false;                // true -> SSL on connect
        public bool UseStartTls { get; set; } = true;            // true -> STARTTLS
        public string? ReplyTo { get; set; }                     // tuỳ chọn
        public int TimeoutMs { get; set; } = 10000;              // 10s
    }

    public sealed class EmailSender : IEmailSender
    {
        private readonly MailSettings _opt;
        private readonly ILogger<EmailSender> _log;

        public EmailSender(IOptions<MailSettings> mailSettings, ILogger<EmailSender> logger)
        {
            _opt = mailSettings.Value;
            _log = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var msg = new MimeMessage();
            msg.Sender = new MailboxAddress(_opt.DisplayName ?? "MotorShop", _opt.Mail);
            msg.From.Add(new MailboxAddress(_opt.DisplayName ?? "MotorShop", _opt.Mail));
            msg.To.Add(MailboxAddress.Parse(email));
            if (!string.IsNullOrWhiteSpace(_opt.ReplyTo))
                msg.ReplyTo.Add(MailboxAddress.Parse(_opt.ReplyTo));
            msg.Subject = subject ?? "(no subject)";

            var body = new BodyBuilder { HtmlBody = htmlMessage ?? string.Empty };
            msg.Body = body.ToMessageBody();

            try
            {
                using var smtp = new SmtpClient
                {
                    Timeout = _opt.TimeoutMs
                };

                // Chọn SecureSocketOptions linh hoạt
                SecureSocketOptions sec;
                if (_opt.UseSsl) sec = SecureSocketOptions.SslOnConnect;
                else if (_opt.UseStartTls) sec = SecureSocketOptions.StartTls;
                else sec = SecureSocketOptions.Auto;

                await smtp.ConnectAsync(_opt.Host, _opt.Port, sec);

                if (!string.IsNullOrEmpty(_opt.Mail) && !string.IsNullOrEmpty(_opt.Password))
                    await smtp.AuthenticateAsync(_opt.Mail, _opt.Password);

                await smtp.SendAsync(msg);
                await smtp.DisconnectAsync(true);

                _log.LogInformation("Email sent to {Email} (subject: {Subject})", email, subject);
            }
            catch (Exception ex)
            {
                // Không throw để không chặn luồng đăng ký/khôi phục mật khẩu
                _log.LogError(ex, "Failed to send email to {Email} (subject: {Subject})", email, subject);
            }
        }
    }
}
