using System.Diagnostics;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using log4net;

namespace TransactionAPI.Messaging
{
    public class RabbitMqHelper : IRabbitMqHelper
    {
        //private static readonly ILog _log4net = LogManager.GetLogger(typeof(RabbitMqHelper));

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
           // _log4net.Info("RabbitMqHelper");
        }
        public IConnection CreateConnection()
        {
            //_log4net.Info("RabbitMqHelper: CreateConnection");
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

           // _log4net.Info("RabbitMqHelper: CreateModelAndDeclareQueue");
            return channel;
        }
        public void StartConsumer(IModel channel, Action<BasicDeliverEventArgs> processMessage)
        {
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (bc, ea) => processMessage(ea);

            channel.BasicConsume(queue: QueueName, autoAck: true, consumer: consumer);

           // _log4net.Info("RabbitMqHelper: StartConsumer");
        }
        public void AddMessagingTags(Activity activity)
        {
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination_kind", "queue");
            activity?.SetTag("messaging.destination", DefaultExchangeName);
            activity?.SetTag("messaging.rabbitmq.routing_key", QueueName);

            //_log4net.Info("RabbitMqHelper: AddMessagingTags");
        }
    }
}
