using System;
namespace WorkerSvc.Configs
{
	public class RabbitMQConfig
	{
		public string queueName { get; set; } = null!;
		public Amqp amqp { get; set; } = null!;
		public Http http { get; set; } = null!;


		public class Amqp 
        {
			public string host { get; set; } = null!;
			public string port { get; set; } = null!;
			public string userName { get; set; } = null!;
			public string password { get; set; } = null!;
		}

		public class Http
		{
			public string host { get; set; } = null!;

			public string port { get; set; } = null!;
			public string userName { get; set; } = null!;
			public string password { get; set; } = null!;
		}
	}
}

