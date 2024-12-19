namespace Res.Domain.Enums
{
    public enum SsrStatus
    {
        Requested,    // Initial request
        Confirmed,    // Confirmed by airline
        Declined,     // Declined by airline
        Cancelled,    // Cancelled by passenger/agent
        Pending,      // Awaiting response
        NoAction      // Information only
    }
}