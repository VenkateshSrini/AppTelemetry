using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;

namespace WorkerSvc.Messaging.Helper
{
    public interface IRabbitMqHelper
    {
        string DefaultExchangeName { get; init; }
        string QueueName { get; init; }

        void AddMessagingTags(Activity activity);
        IConnection CreateConnection();
        IModel CreateModelAndDeclareQueue(IConnection connection);
        void StartConsumer(IModel channel, Action<BasicDeliverEventArgs> processMessage);
    }
}
