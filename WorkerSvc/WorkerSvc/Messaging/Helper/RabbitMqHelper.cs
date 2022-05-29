using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerSvc.Messaging.Helper
{
    public class RabbitMqHelper : IRabbitMqHelper
    {
        public string DefaultExchangeName { get; init; } = "";
        public string QueueName { get; init; }
        private readonly ConnectionFactory ConnectionFactory;
        private readonly ILogger<RabbitMqHelper> _logger;

        public RabbitMqHelper(ILogger<RabbitMqHelper> logger,
            IConfiguration configuration)
        {
            ConnectionFactory = new ConnectionFactory()
            {
                HostName = configuration["Rabbitmq:HostName"],
                UserName = configuration["Rabbitmq:UserName"],
                Password = configuration["Rabbitmq:Password"],
                Port = int.Parse(configuration["Rabbitmq:Port"]),
                RequestedConnectionTimeout = TimeSpan.FromMilliseconds(
                    int.Parse(configuration["Rabbitmq:Timeout"])),
            };
            _logger = logger;
            QueueName = configuration["Rabbitmq:QueueName"];
            _logger.LogInformation("RabbitMqHelper");
        }
        public IConnection CreateConnection()
        {
            _logger.LogInformation("RabbitMqHelper: CreateConnection");
            return ConnectionFactory.CreateConnection();
        }
        public IModel CreateModelAndDeclareQueue(IConnection connection)
        {
            var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: QueueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("RabbitMqHelper: CreateModelAndDeclareQueue");
            return channel;
        }
        public void StartConsumer(IModel channel, Action<BasicDeliverEventArgs> processMessage)
        {
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (bc, ea) => processMessage(ea);

            channel.BasicConsume(queue: QueueName, autoAck: true, consumer: consumer);

            _logger.LogInformation("RabbitMqHelper: StartConsumer");
        }
        public void AddMessagingTags(Activity activity)
        {
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination_kind", "queue");
            activity?.SetTag("messaging.destination", DefaultExchangeName);
            activity?.SetTag("messaging.rabbitmq.routing_key", QueueName);

            _logger.LogInformation("RabbitMqHelper: AddMessagingTags");
        }
    }
}
