namespace Res.Api.Offer.Domain.Models
{
    public class FlightAvailability
    {
        public string FlightNumber { get; set; }
        public string AircraftType { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int AvailableSeats { get; set; }
    }
}