using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Invoice_and_Payment_System.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string recipient, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;


        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            try
            {
                var email = _configuration.GetValue<string>("EmailConfiguration:Email");
                var password = _configuration.GetValue<string>("EmailConfiguration:Password");
                var host = _configuration.GetValue<string>("EmailConfiguration:Host");
                var port = _configuration.GetValue<int>("EmailConfiguration:Port");

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(host) || port <= 0)
                {
                    throw new InvalidOperationException("Email configuration is incomplete");
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Invoice and Payment System", email));
                message.To.Add(new MailboxAddress("", recipient));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(email, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(ex, "SMTP error sending email to {Recipient}", recipient);
                throw new ApplicationException($"Failed to send email: {ex.StatusCode} - {ex.Message}", ex);

            }
            catch (SmtpProtocolException ex)
            {
                _logger.LogError(ex, "SMTP protocol error sending email to {Recipient}", recipient);
                throw new ApplicationException("An unexpected error occurred while sending email", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email to {Recipient}", recipient);
                throw new ApplicationException("An unexpected error occurred while sending email", ex);
            }
        }
    }
}
