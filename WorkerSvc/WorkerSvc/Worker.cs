using WorkerSvc.Configs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;

namespace WorkerSvc;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMQConfig _rabbitMQConfig;
    private readonly IConnection _connection;
    private ActivitySource activitySource; 
    private Activity activity;
    private IModel _channel;
    private static readonly TextMapPropagator Propagator = new TraceContextPropagator();

    public Worker(ILogger<Worker> logger, IConfiguration configuration, 
        IOptionsMonitor<RabbitMQConfig> rabbitmqConfigOptions)
    {
        _logger = logger;
        var serviceName = "BatchApp";
         activitySource = new ActivitySource(serviceName);

         activity = activitySource.StartActivity(nameof(Worker));

        _rabbitMQConfig = rabbitmqConfigOptions?.CurrentValue;
        var connectionFactory = new ConnectionFactory
        {
            Uri = new Uri($"amqp://{_rabbitMQConfig.amqp.host}"),
            UserName = _rabbitMQConfig.amqp.userName,
            Password = _rabbitMQConfig.amqp.password
        };
        _connection = connectionFactory.CreateConnection();
        _logger.LogInformation("abcd");


    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateModel();
        var MyActivitySource = new ActivitySource("BatchApp");

        using var activity = MyActivitySource.StartActivity("Execute Async");

        activity?.AddTag("Queue Name", _rabbitMQConfig.queueName);
        _channel.QueueDeclare(queue: _rabbitMQConfig.queueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        var messageCount = _channel.MessageCount(_rabbitMQConfig.queueName);
        activity?.AddTag("Message Count", messageCount);
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += Message_Received;
        _channel.BasicConsume(queue: _rabbitMQConfig.queueName,
                  autoAck: false,
                  consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void Message_Received(object? model, BasicDeliverEventArgs ea)
    {
        var path = Directory.GetCurrentDirectory();
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());

        var parentContext = Propagator.Extract(default, ea.BasicProperties, ExtractTraceContextFromBasicProperties);
        Baggage.Current = parentContext.Baggage;

        AddActivityTags();
        var message = body.ToString();
        
        Console.WriteLine(" logging content to file");
        File.AppendAllText($@"{path}\file.log", message);
        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
    }

    private void AddActivityTags()
    {
        activity?.SetTag("messaging.system","rabbimq");
        activity?.SetTag("messaging.operation", "Receiver");
    }

    private  IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
    {
        try
        {
            if (props.Headers.TryGetValue(key, out var value))
            {
                var bytes = value as byte[];
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return Enumerable.Empty<string>();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Dispose();
        _connection.Dispose();

        return base.StopAsync(cancellationToken);
    }


}

