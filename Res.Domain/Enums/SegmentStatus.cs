namespace Res.Domain.Enums
{
    public enum SegmentStatus
    {
        Holding,      // Seat held but not confirmed
        Confirmed,    // HK - Confirmed booking
        Waitlisted,   // HL - Waitlisted
        RequestPending, // NN - Need confirmation
        Cancelled,    // XX - Cancelled
        Flown         // FK - Flown
    }
}