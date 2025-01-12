namespace Res.Api.Offer.Domain.Models
{
    public class Flight
    {
        public string FlightNo { get; set; }
        public string AircraftType { get; set; }
        public DateTime DepartureDateTime { get; set; }
        public DateTime ArrivalDateTime { get; set; }
        public List<Offer> Offers { get; set; }
    }
}