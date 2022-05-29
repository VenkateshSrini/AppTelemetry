using System;
namespace WorkerSvc.Configs
{

    public class ManagementConfig
    {
        public Tracing Tracing { get; set; }
    }


    public class Tracing
    {
        public Exporter Exporter { get; set; }
    }

    public class Exporter
    {
        public ZipkinConfig Zipkin { get; set; }
        public JaegerConfig Jaeger { get; set; }
    }

    public class Jaeger
    {
        public string AgentHost { get; set; }
        public string AgentPort { get; set; }
    }

    public class Zipkin
    {
        public string EndPoint { get; set; }
    }

}

