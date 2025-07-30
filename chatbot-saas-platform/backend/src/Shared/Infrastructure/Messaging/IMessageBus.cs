using System;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Messaging;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, string routingKey = "", string exchange = "") where T : class;
    Task SubscribeAsync<T>(Func<T, Task> handler, string queueName, string exchange = "", string routingKey = "") where T : class;
    Task SubscribeAsync<T>(Func<T, Task<bool>> handler, string queueName, string exchange = "", string routingKey = "") where T : class;
    void Dispose();
}

public interface IMessageHandler<T> where T : class
{
    Task<bool> HandleAsync(T message);
}
