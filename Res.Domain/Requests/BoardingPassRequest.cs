namespace Res.Domain.Requests
{
    public class BoardingPassRequest
    {
        public string RecordLocator { get; set; }
        public int PassengerId { get; set; }
        public string TicketNumber { get; set; }
        public string FlightNumber { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string DepartureDate { get; set; }
        public string DepartureTime { get; set; }
        public string SeatNumber { get; set; }
        public bool HasCheckedBags { get; set; }
        public decimal BaggageWeight { get; set; }
        public int BaggageCount { get; set; }
        public string DepartureGate { get; set; }
        public List<string> SsrCodes { get; set; } = new();
    }
}