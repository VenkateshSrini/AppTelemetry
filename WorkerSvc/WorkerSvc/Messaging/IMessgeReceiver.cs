using RabbitMQ.Client.Events;

namespace WorkerSvc.Messaging
{
    public interface IMessgeReceiver
    {
        void Dispose();
        void StartConsumer();
    }
}