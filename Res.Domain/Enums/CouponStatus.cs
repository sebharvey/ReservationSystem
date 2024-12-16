namespace Res.Domain.Enums
{
    public enum CouponStatus
    {
        Open,           // O - Available for use
        Used,           // F - Flight has been flown
        Exchanged,      // E - Exchanged for new ticket
        Refunded,       // R - Money returned to passenger
        Void,           // V - Cancelled before any use 
        Suspended,      // S - Temporarily suspended
        AirportControl, // A - Under airport control
        CheckedIn,      // C - Passenger checked in
        Lifted,         // L - Boarding pass issued
        IrregularOps,   // I - Flight disruption/schedule change
        Printed,        // P - Ticket printed but not used
        Conjunction,    // J - Part of a conjunction ticket
        NotValid        // N - Not valid for travel
    }
}