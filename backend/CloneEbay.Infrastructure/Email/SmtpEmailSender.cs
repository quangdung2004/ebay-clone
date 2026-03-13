using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CloneEbay.Application.Notifications;

namespace CloneEbay.Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string html, CancellationToken ct)
        {
            try
            {
                var host = _config["Email:Host"];
                var portStr = _config["Email:Port"];
                var port = string.IsNullOrEmpty(portStr) ? 1025 : int.Parse(portStr);
                var user = _config["Email:User"] ?? "";
                var pass = _config["Email:Pass"] ?? "";
                var from = _config["Email:From"] ?? "noreply@clone-ebay.com";

                var client = new SmtpClient(host, port)
                {
                    EnableSsl = false
                };
                
                if (!string.IsNullOrEmpty(user))
                {
                    client.Credentials = new NetworkCredential(user, pass);
                }

                var message = new MailMessage(from, to, subject, html)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(message, ct);
                _logger.LogInformation("Successfully sent email to {To} with subject {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", to, subject);
                throw; // Rethrow to let caller know
            }
        }
    }
}
