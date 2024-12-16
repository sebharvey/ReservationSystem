using Res.Domain.Enums;

namespace Res.Domain.Entities.Pnr
{
    public class Segment
    {
        public string FlightNumber { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string DepartureDate { get; set; }
        public string DepartureTime { get; set; }
        public string ArrivalDate { get; set; }
        public string ArrivalTime { get; set; }
        public string BookingClass { get; set; }
        public SegmentStatus Status { get; set; }
        public int Quantity { get; set; }
        public bool IsSurfaceSegment { get; set; }
    }
}