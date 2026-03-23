using System.Text;
using System.Text.Json;
using CloneEbay.Application.Notifications;
using CloneEbay.Contracts.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace CloneEbay.Infrastructure.Messaging;

public sealed class RabbitMqAuctionEventPublisher : IAuctionEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string QueueName = "auction.winner.email";
    private readonly ILogger<RabbitMqAuctionEventPublisher> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RabbitMqAuctionEventPublisher(
        IConfiguration config,
        ILogger<RabbitMqAuctionEventPublisher> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        var hostName = config["RabbitMQ:HostName"] ?? "localhost";
        var portStr = config["RabbitMQ:Port"];
        var port = string.IsNullOrEmpty(portStr) ? 5672 : int.Parse(portStr);
        var userName = config["RabbitMQ:UserName"] ?? "guest";
        var password = config["RabbitMQ:Password"] ?? "guest";

        _logger.LogInformation(
            "Connecting to RabbitMQ | host={host} | port={port}",
            hostName,
            port);

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public Task PublishWinnerEmailAsync(int productId, int winnerUserId, decimal winningBid, int orderId, CancellationToken ct)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var correlationId =
                httpContext?.Items["X-Correlation-Id"]?.ToString()
                ?? httpContext?.TraceIdentifier
                ?? Guid.NewGuid().ToString("N");

            var transactionId =
                httpContext?.Items["X-Transaction-Id"]?.ToString()
                ?? correlationId;

            var payload = new AuctionWinnerEmailMessage(productId, winnerUserId, winningBid, orderId);
            var json = JsonSerializer.Serialize(payload);
            var body = Encoding.UTF8.GetBytes(json);

            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.CorrelationId = correlationId;
            props.MessageId = transactionId;
            props.Headers ??= new Dictionary<string, object>();
            props.Headers["x-correlation-id"] = correlationId;
            props.Headers["x-transaction-id"] = transactionId;

            _channel.BasicPublish(
                exchange: "",
                routingKey: QueueName,
                basicProperties: props,
                body: body);

            _logger.LogInformation(
                "RabbitMQ publish succeeded | cid={cid} | tx={tx} | queue={queue} | productId={productId} | orderId={orderId}",
                correlationId,
                transactionId,
                QueueName,
                productId,
                orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "RabbitMQ publish failed | queue={queue} | productId={productId} | orderId={orderId}",
                QueueName,
                productId,
                orderId);
            throw;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}