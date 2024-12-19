namespace Res.Domain.Entities.Pnr
{
    public class SeatAssignment
    {
        public int PassengerId { get; set; }
        public string SegmentNumber { get; set; }
        public string SeatNumber { get; set; }
        public DateTime AssignedDate { get; set; }
    }
}