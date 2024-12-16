namespace Res.Domain.Exceptions
{
    public class TicketingException : Exception
    {
        public TicketingException(string message) : base(message) { }

        public TicketingException(string message, Exception innerException) : base(message, innerException) { }
    }
}