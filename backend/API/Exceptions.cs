namespace SupportBackend
{
    public class UnknownMessengerTypeException : Exception
    {
        public UnknownMessengerTypeException(string message) : base(message) { }
    }

    public class InvalidVkMessageException : Exception
    {
        public InvalidVkMessageException(string message) : base(message) { }
    }

    public class InvalidClientMessageException : Exception
    {
        public InvalidClientMessageException(string message) : base(message) { }
    }
}
