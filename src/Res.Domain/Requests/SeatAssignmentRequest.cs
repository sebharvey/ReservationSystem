namespace Res.Domain.Requests
{
    public class SeatAssignmentRequest
    {
        public string SeatNumber { get; set; }
        public int PassengerId { get; set; }
        public string SegmentNumber { get; set; }
    }
}