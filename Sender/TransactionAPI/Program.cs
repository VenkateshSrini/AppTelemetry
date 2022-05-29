using Steeltoe.Extensions.Configuration.Kubernetes;
using Steeltoe.Management.Kubernetes;
using Steeltoe.Management.Tracing;
using Steeltoe.Extensions.Logging;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Exporter;
using OpenTelemetry;
using TransactionAPI.Messaging;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using log4net;
using System.Reflection;


// Adding log4net
var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
FileInfo fileInfo = new FileInfo("log4net.config");

log4net.Config.XmlConfigurator.Configure(repository, fileInfo);


var builder = WebApplication.CreateBuilder(args);


// Steeltoe Kubernetes
builder.Host.ConfigureAppConfiguration(x => x.AddKubernetes());
builder.Host.AddKubernetesConfiguration();
builder.Host.AddKubernetesActuators();
builder.Logging.AddDynamicConsole();


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDistributedTracingAspNetCore();


// Defining properties of AspNetCoreInstrumentation
builder.Services.PostConfigure<AspNetCoreInstrumentationOptions>(options =>
{
    options.Enrich = (activity, eventName, rawObject) =>
    {
        if (eventName.Equals("OnStartActivity"))
        {
            if (rawObject is HttpRequest httpRequest)
            {
                activity.SetTag("requestProtocol", httpRequest.Protocol);
            }
        }
    };
});


// Defining properties of JaegerExporter
builder.Services.PostConfigure<JaegerExporterOptions>(options =>
{
    options.ExportProcessorType = ExportProcessorType.Batch;
    options.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = 1000;
    
});


// Adding Jaeger And Zipkin with the above defined properties
var JaegerHost = builder.Configuration["Jaeger:Hostname"];
var JaegerPort = builder.Configuration["Jaeger:Port"];
var ZipkinHost = builder.Configuration["Zipkin:Hostname"];
var ZipkinPort = builder.Configuration["Zipkin:Port"];


builder.Services.AddOpenTelemetryTracing(x =>
{
    x.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TransactionAPI"))
    .AddAspNetCoreInstrumentation(sam => sam.Filter = httpContext => !httpContext.Request.Path.Value?.Contains("/_framework/aspnetcore-browser-refresh.js") ?? true)
    .AddHttpClientInstrumentation(sam => sam.Enrich = (activity, eventName, rawObject) =>
    {
        if (eventName == "OnStartActivity" && rawObject is HttpRequestMessage request && request.Method == HttpMethod.Get)
            activity.SetTag("requestPolicy", request.VersionPolicy);
    })
    //.AddZipkinExporter(sam =>
    //{
    //    sam.Endpoint = new Uri($"http://{ZipkinHost}:{ZipkinPort}/api/v2/spans");
    //})
    .AddJaegerExporter(sam =>
    {
        sam.AgentPort = int.Parse(JaegerPort);
        sam.AgentHost = JaegerHost;
        sam.ExportProcessorType = ExportProcessorType.Simple;
      
    });
});



// Adding Classes And Interfaces
builder.Services.AddSingleton<IRabbitMqHelper, RabbitMqHelper>();
builder.Services.AddSingleton<IMessageSender, MessageSender>();


var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();


app.Run();
