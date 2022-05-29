using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerSvc.Messaging;

namespace WorkerSvc
{
    public class TelemetryService : BackgroundService
    {
        private readonly ILogger<TelemetryService> _logger;
        private readonly IMessgeReceiver _messgeReceiver;
        public TelemetryService(ILogger<TelemetryService> logger, IMessgeReceiver messgeReceiver)
        {
            _logger = logger;
            _messgeReceiver = messgeReceiver;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            _messgeReceiver.StartConsumer();
            await Task.CompletedTask;
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _messgeReceiver.Dispose();
            return base.StopAsync(cancellationToken);
        }
    }
}
