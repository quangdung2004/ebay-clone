using System.Text;
using System.Text.Json;
using CloneEbay.Application.Notifications;
using CloneEbay.Contracts.Messaging;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CloneEbay.Infrastructure.Messaging;

public sealed class AuctionWinnerEmailConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionWinnerEmailConsumer> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;
    private const string QueueName = "auction.winner.email";

    public AuctionWinnerEmailConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<AuctionWinnerEmailConsumer> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var hostName = _config["RabbitMQ:HostName"] ?? "localhost";
        var portStr = _config["RabbitMQ:Port"];
        var port = string.IsNullOrEmpty(portStr) ? 5672 : int.Parse(portStr);
        var userName = _config["RabbitMQ:UserName"] ?? "guest";
        var password = _config["RabbitMQ:Password"] ?? "guest";

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password,
            DispatchConsumersAsync = true
        };

        try 
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.BasicQos(0, 1, false);
            _logger.LogInformation("RabbitMQ Consumer connected successfully to {Host}:{Port}", hostName, port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ Consumer failed to connect to {Host}:{Port}", hostName, port);
            throw;
        }

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel was not initialized.");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("Received message from queue {QueueName}: {Json}", QueueName, json);
                
                var message = JsonSerializer.Deserialize<AuctionWinnerEmailMessage>(json);

                if (message == null)
                {
                    _logger.LogWarning("Failed to deserialize message. Acknowledging safely.");
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CloneEbayDbContext>();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var user = await db.User
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.id == message.WinnerUserId, stoppingToken);

                if (user == null || string.IsNullOrWhiteSpace(user.email))
                {
                    _logger.LogWarning("User {UserId} not found or email is empty. Acking.", message.WinnerUserId);
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                var product = await db.Product
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.id == message.ProductId, stoppingToken);

                var subject = "You won the auction!";
                var html = $@"
<h2>Congratulations!</h2>
<p>You won the auction for product ID <strong>{message.ProductId}</strong>.</p>
<p>Winning bid: <strong>{message.WinningBid:N0}</strong></p>
<p>Order ID: <strong>{message.OrderId}</strong></p>
<p>Product: <strong>{product?.title ?? "N/A"}</strong></p>
<p>Please complete payment as soon as possible.</p>";

                _logger.LogInformation("Sending winner email to {Email}", user.email);
                await emailSender.SendAsync(user.email!, subject, html, stoppingToken);
                _logger.LogInformation("Email sent successfully.");

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process auction winner email message. Nacking.");
                _channel!.BasicNack(ea.DeliveryTag, false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}