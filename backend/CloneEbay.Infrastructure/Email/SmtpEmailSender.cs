using System.Net;
using System.Net.Mail;
using CloneEbay.Application.Common.Diagnostics;
using CloneEbay.Application.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CloneEbay.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly ITransactionContextAccessor _txContext;

    public SmtpEmailSender(
        IConfiguration config,
        ILogger<SmtpEmailSender> logger,
        ITransactionContextAccessor txContext)
    {
        _config = config;
        _logger = logger;
        _txContext = txContext;
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

            _logger.LogInformation(
                "SMTP send started | cid={cid} | tx={tx} | host={host} | port={port} | to={to} | subject={subject}",
                _txContext.CorrelationId,
                _txContext.TransactionId,
                host,
                port,
                to,
                subject);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = false
            };

            if (!string.IsNullOrEmpty(user))
            {
                client.Credentials = new NetworkCredential(user, pass);
            }

            using var message = new MailMessage(from, to, subject, html)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(message, ct);

            _logger.LogInformation(
                "SMTP send succeeded | cid={cid} | tx={tx} | to={to} | subject={subject}",
                _txContext.CorrelationId,
                _txContext.TransactionId,
                to,
                subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SMTP send failed | cid={cid} | tx={tx} | to={to} | subject={subject}",
                _txContext.CorrelationId,
                _txContext.TransactionId,
                to,
                subject);
            throw;
        }
    }
}