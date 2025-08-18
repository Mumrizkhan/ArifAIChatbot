using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Messaging;

public class RabbitMQMessageBus : IMessageBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMQMessageBus> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed = false;

    public RabbitMQMessageBus(IConfiguration configuration, ILogger<RabbitMQMessageBus> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost",
                Port = int.Parse(configuration.GetValue<string>("RabbitMQ:Port") ?? "5672"),
                UserName = configuration.GetValue<string>("RabbitMQ:Username") ?? "guest",
                Password = configuration.GetValue<string>("RabbitMQ:Password") ?? "guest",
                VirtualHost = configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;

            _logger.LogInformation("RabbitMQ connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            throw;
        }
    }

    public async Task PublishAsync<T>(T message, string routingKey = "", string exchange = "") where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RabbitMQMessageBus));

        try
        {
            var exchangeName = string.IsNullOrEmpty(exchange) ? "chatbot.events" : exchange;
            var messageRoutingKey = string.IsNullOrEmpty(routingKey) ? typeof(T).Name.ToLowerInvariant() : routingKey;

            await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: true);

            var messageBody = JsonSerializer.Serialize(message, _jsonOptions);
            var body = Encoding.UTF8.GetBytes(messageBody);

            //Create basic properties for the message

            var properties = new BasicProperties();
            properties.Persistent = true;
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = typeof(T).Name;

            await _channel.BasicPublishAsync(
                 exchange: exchangeName,
                 routingKey: messageRoutingKey,
                 mandatory: false,
                 basicProperties: properties,
                 body: body);

            _logger.LogDebug("Message published: {MessageType} to {Exchange}/{RoutingKey}",
                typeof(T).Name, exchangeName, messageRoutingKey);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message: {MessageType}", typeof(T).Name);
            throw;
        }
    }

    public async Task SubscribeAsync<T>(Func<T, Task> handler, string queueName, string exchange = "", string routingKey = "") where T : class
    {
        await SubscribeAsync<T>(async message =>
        {
            await handler(message);
            return true;
        }, queueName, exchange, routingKey);
    }

    public async Task SubscribeAsync<T>(Func<T, Task<bool>> handler, string queueName, string exchange = "", string routingKey = "") where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RabbitMQMessageBus));

        try
        {
            var exchangeName = string.IsNullOrEmpty(exchange) ? "chatbot.events" : exchange;
            var messageRoutingKey = string.IsNullOrEmpty(routingKey) ? typeof(T).Name.ToLowerInvariant() : routingKey;

            await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, durable: true);
            await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(queueName, exchangeName, messageRoutingKey);
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);

                try
                {
                    var message = JsonSerializer.Deserialize<T>(messageJson, _jsonOptions);
                    if (message != null)
                    {
                        var success = await handler(message);
                        if (success)
                        {
                            await _channel.BasicAckAsync(ea.DeliveryTag, false);
                            _logger.LogDebug("Message processed successfully: {MessageType}", typeof(T).Name);
                        }
                        else
                        {
                            await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                            _logger.LogWarning("Message processing failed, requeued: {MessageType}", typeof(T).Name);
                        }
                    }
                    else
                    {
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                        _logger.LogError("Failed to deserialize message: {MessageType}", typeof(T).Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message: {MessageType}", typeof(T).Name);
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("Subscribed to queue: {QueueName} for message type: {MessageType}",
                queueName, typeof(T).Name);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to queue: {QueueName}", queueName);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _channel?.CloseAsync();
            _channel?.Dispose();
            _connection?.CloseAsync();
            _connection?.Dispose();
            _disposed = true;
            _logger.LogInformation("RabbitMQ connection disposed");
        }
    }
}
