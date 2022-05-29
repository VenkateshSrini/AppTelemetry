using WorkerSvc;
using WorkerSvc.Configs;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Diagnostics;
using WorkerSvc.Messaging.Helper;
using WorkerSvc.Messaging;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configBuilder => {
        configBuilder.AddJsonFile("appsettings.json")
                     .AddEnvironmentVariables();
    })
    .ConfigureServices((hostContext,services) =>
    {
        var serviceName = "BatchApp";
        var serviceVersion = "1.0.0";
        var managementConfig = new ManagementConfig();
        hostContext.Configuration.GetSection("Management").Bind(managementConfig);
        //services.AddHostedService<Worker>();
        services.AddHostedService<TelemetryService>();
        services.Configure<RabbitMQConfig>(hostContext.Configuration.GetSection("RabbitMq"));
        services.AddTraceActuatorServices(hostContext.Configuration, Steeltoe.Management.Endpoint.MediaTypeVersion.V2);
        services.AddOpenTelemetryTracing(traceProviderBuilder => {
            traceProviderBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TransactionAPI"));
            traceProviderBuilder.AddSource(serviceName);
            traceProviderBuilder.AddJaegerExporter(x =>
            {
                x.AgentHost = managementConfig.Tracing.Exporter.Jaeger.AgentHost;
                x.AgentPort = managementConfig.Tracing.Exporter.Jaeger.AgentPort;
                x.ExportProcessorType = ExportProcessorType.Simple;
                //x.MaxPayloadSizeInBytes = 4096;
                //x.ExportProcessorType = ExportProcessorType.Batch;
                //x.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>()
                //{
                //    MaxQueueSize = 2048,
                //    ScheduledDelayMilliseconds = 5000,
                //    ExporterTimeoutMilliseconds = 30000,
                //    MaxExportBatchSize = 512,
                //};

            });
            //traceProviderBuilder.AddZipkinExporter(zipkin =>
            //{
            //    zipkin.Endpoint = managementConfig.Tracing.Exporter.Zipkin.EndPoint;
            //});
        });
        services.AddSingleton<IRabbitMqHelper, RabbitMqHelper>();
        services.AddSingleton<IMessgeReceiver, MessgeReceiver>();
        
    
    })
    .Build();

await host.RunAsync();

//IConfiguration config = new ConfigurationBuilder()
//    .AddJsonFile("appsettings.json")
//    .AddEnvironmentVariables()
//    .Build();

//ManagementConfig _managementConfig = config.GetRequiredSection("Management").Get<ManagementConfig>();

//using var tracerProvider = Sdk.CreateTracerProviderBuilder()
//    .AddSource(nameof(Program))

//    .SetResourceBuilder(ResourceBuilder.CreateDefault())
//    .SetResourceBuilder(
//        ResourceBuilder.CreateDefault()
//    .AddService(serviceName: serviceName, serviceVersion: serviceVersion.ToString()))
//    .AddConsoleExporter()
//    .AddJaegerExporter(x =>
//    {
//        x.AgentHost = _managementConfig.Tracing.Exporter.Jaeger.AgentHost;
//        x.AgentPort = Convert.ToInt32(_managementConfig.Tracing.Exporter.Jaeger.AgentPort);

//    })
//.AddZipkinExporter(zipkin =>
//{
//    zipkin.Endpoint = _managementConfig.Tracing.Exporter.Zipkin.EndPoint;
//})
//.Build();



