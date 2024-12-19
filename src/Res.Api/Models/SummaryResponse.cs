namespace Res.Api.Models
{
    public class SummaryResponse
    {
        public List<SummarySegment> Segments { get; set; }


        public class SummarySegment
        {
            public string FlightNumber { get; set; }
            public DateTime DepartureDate { get; set; }
            public string BookingClass { get; set; }
            public string OfferId { get; set; }
        }
    }
}
