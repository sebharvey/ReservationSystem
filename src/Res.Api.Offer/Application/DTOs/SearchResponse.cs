namespace Res.Api.Offer.Application.DTOs
{
    public class SearchResponse
    {
        public string From { get; set; }
        public string To { get; set; }
        public DateTime Date { get; set; }
        public List<FlightResult> Flights { get; set; }
    }

    public class FlightResult
    {
        public string FlightNo { get; set; }
        public DateTime DepartureDateTime { get; set; }
        public DateTime ArrivalDateTime { get; set; }
        public string AircraftType { get; set; }
        public List<Offer> Offers { get; set; }
    }

    public class Offer
    {
        public bool IsAvailable { get; set; }
        public string BookingClass { get; set; }
        public decimal Price { get; set; }
        public string OfferId { get; set; }
    }
}