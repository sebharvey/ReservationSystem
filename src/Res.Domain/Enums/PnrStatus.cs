namespace Res.Domain.Enums
{
    public enum PnrStatus
    {
        Pending,      // Initial creation
        Confirmed,    // Fully booked
        Ticketed,     // Tickets issued
        Cancelled,    // Cancelled booking
        Flown,        // Travel completed
        NoShow        // Passenger didn't show up
    }
}