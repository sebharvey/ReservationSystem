using Res.Domain.Entities.Pnr;
using Res.Domain.Enums;

namespace Res.Domain.Entities.CheckIn
{
    public class BoardingPass
    {
        public int PassengerId { get; set; }
        public string TicketNumber { get; set; }
        public string FlightNumber { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string DepartureDate { get; set; }
        public string DepartureTime { get; set; }
        public string DepartureGate { get; set; }
        public string SeatNumber { get; set; }
        public string BoardingGroup { get; set; }
        public string Sequence { get; set; }
        public bool HasCheckedBags { get; set; }
        public decimal BaggageWeight { get; set; }
        public int BaggageCount { get; set; }
        public CheckInStatus Status { get; set; }
        public DateTime CheckInTime { get; set; }
        public string BarcodeData { get; set; }
        public string FastTrack { get; set; }
        public string LoungeAccess { get; set; }
        public List<string> SecurityMessages { get; set; } = new();
        public List<string> RegulatoryMessages { get; set; } = new();
        public Passenger Passenger { get; set; }
        public string BookingClass { get; set; }
    }
}