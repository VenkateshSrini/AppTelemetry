using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;
using log4net;

namespace TransactionAPI.Messaging
{
    public class MessageSender : IDisposable, IMessageSender
    {
        private static readonly ILog _log4net = LogManager.GetLogger(typeof(MessageSender));

        private  readonly ActivitySource ActivitySource = new(nameof(MessageSender));
        private  readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        private readonly ILogger<MessageSender> _logger;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IRabbitMqHelper _rabbitMqHelper;
        public MessageSender(ILogger<MessageSender> logger, IRabbitMqHelper rabbitMqHelper)
        {
            _logger = logger;
            _connection = rabbitMqHelper.CreateConnection();
            _channel = rabbitMqHelper.CreateModelAndDeclareQueue(_connection);
            _rabbitMqHelper = rabbitMqHelper;

            _log4net.Info("MessageSender");
            var activityListener = new ActivityListener
            {
                ShouldListenTo = s => true,
                SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
                Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(activityListener);
        }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();

            _log4net.Info("MessageSender: Dispose");
        }

        public bool SendMessage(string messageBody)
        {
            try
            {
                // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
                // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#span-name
                var activityName = $"{_rabbitMqHelper.QueueName} send";

                using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Producer);
                var props = _channel.CreateBasicProperties();

                // Depending on Sampling (and whether a listener is registered or not), the
                // activity above may not be created.
                // If it is created, then propagate its context.
                // If it is not created, the propagate the Current context,
                // if any.
                ActivityContext contextToInject = default;
                if (activity != null)
                {
                    contextToInject = activity.Context;
                }
                else if (Activity.Current != null)
                {
                    contextToInject = Activity.Current.Context;
                }

                // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
                Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), props, this.InjectTraceContextIntoBasicProperties);

                // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
                _rabbitMqHelper.AddMessagingTags(activity);
                var body = messageBody;

                _channel.BasicPublish(
                    exchange: _rabbitMqHelper.DefaultExchangeName,
                    routingKey: _rabbitMqHelper.QueueName,
                    basicProperties: props,
                    body: Encoding.UTF8.GetBytes(body));

                _logger.LogInformation($"Message sent: [{body}]");
                
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Message publishing failed.");
                //_log4net.Fatal("MessageSender: SendMessage: Message publishing has failed.");

                throw;
            }
        }

        private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
        {
            try
            {
                if (props.Headers == null)
                {
                    props.Headers = new Dictionary<string, object>();
                }

                props.Headers[key] = value;

                _log4net.Info("MessageSender: InjectTraceContextIntoBasicProperties");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to inject trace context.");
                _log4net.Fatal("MessageSender: InjectTraceContextIntoBasicProperties: Failed to inject trace");
            }
        }
    }
}
