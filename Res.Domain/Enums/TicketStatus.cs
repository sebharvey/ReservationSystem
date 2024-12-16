namespace Res.Domain.Enums
{
    public enum TicketStatus
    {
        Valid,        // OK to fly
        Used,         // Already flown
        Voided,       // Cancelled before use
        Refunded,     // Money returned
        Suspended     // Temporarily invalid
    }
}