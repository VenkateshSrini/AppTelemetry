using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerSvc.Messaging.Helper;

namespace WorkerSvc.Messaging
{
    public class MessgeReceiver : IDisposable, IMessgeReceiver
    {
        private readonly static ActivitySource ActivitySource = new(nameof(MessgeReceiver));
        private readonly static TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        private readonly ILogger<MessgeReceiver> _logger;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IRabbitMqHelper _rabbitMqHelper;
        public MessgeReceiver(ILogger<MessgeReceiver> logger, IRabbitMqHelper rabbitMqHelper)
        {
            _logger = logger;
            _connection = rabbitMqHelper.CreateConnection();
            _channel = rabbitMqHelper.CreateModelAndDeclareQueue(_connection);
            _rabbitMqHelper = rabbitMqHelper;
            var activityListener = new ActivityListener
            {
                ShouldListenTo = s => true,
                SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
                Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(activityListener);
        }
        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();

            _logger.LogInformation("MessageSender: Dispose");
        }
        public void StartConsumer()
        {
            _rabbitMqHelper.StartConsumer(_channel, this.ReceiveMessage);
        }
        public void ReceiveMessage(BasicDeliverEventArgs ea)
        {
            // Extract the PropagationContext of the upstream parent from the message headers.
            //var parentContext = Propagator.Extract(default, ea.BasicProperties, this.ExtractTraceContextFromBasicProperties);
           // Baggage.Current = parentContext.Baggage;

            // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
            // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#span-name
            var activityName = $"{ea.RoutingKey} receive";
            var packetHeaders = ea.BasicProperties.Headers;
            string parentId = string.Empty;
            if ((packetHeaders != null) && (packetHeaders.Count > 0))
            {
                var bytes = (byte[])packetHeaders["traceparent"];
                parentId =  Encoding.Default.GetString(bytes);
            }
            using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Consumer, parentId);

            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.Span.ToArray());

                _logger.LogInformation($"Message received: [{message}]");

                activity?.SetTag("message", message);

                // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
                _rabbitMqHelper.AddMessagingTags(activity);

                // Simulate some work
                //Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Message processing failed.");
            }
        }

        private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
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
                _logger.LogError(ex, "Failed to extract trace context: {ex}");
            }

            return Enumerable.Empty<string>();
        }
    }
}
