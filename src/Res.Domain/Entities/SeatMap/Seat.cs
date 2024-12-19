namespace Res.Domain.Entities.SeatMap
{
    public class Seat
    {
        public string SeatNumber { get; set; }
        public string Status { get; set; } // A=Available, X=Occupied, B=Blocked
        public bool IsExit { get; set; }
        public bool IsBulkhead { get; set; }
        public bool IsAisle { get; set; }
        public bool IsWindow { get; set; }
        public string BlockedReason { get; set; }
    }
}