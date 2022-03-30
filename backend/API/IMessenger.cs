namespace SupportBackend
{
    public interface IMessenger
    {
        public void Send(Message msg);
        public void StartHandlingMessages();
    }
}
