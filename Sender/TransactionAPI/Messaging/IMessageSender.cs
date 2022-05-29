namespace TransactionAPI.Messaging
{
    public interface IMessageSender
    {
        void Dispose();
        bool SendMessage(string messageBody);
    }
}