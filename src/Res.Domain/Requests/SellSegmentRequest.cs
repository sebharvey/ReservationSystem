namespace Res.Domain.Requests
{
    public class SellSegmentRequest
    {
        public string FlightNumber { get; set; }
        public string BookingClass { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string DepartureDate { get; set; }
        public string ArrivalDate { get; set; }
        public string DepartureTime { get; set; }
        public string ArrivalTime { get; set; }
        public int Quantity { get; set; } = 1;
        public int LineNumber { get; set; }
    }
}