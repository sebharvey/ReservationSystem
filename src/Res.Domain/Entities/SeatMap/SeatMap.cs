namespace Res.Domain.Entities.SeatMap
{
    public class SeatMap
    {
        public string FlightNumber { get; set; }
        public string DepartureDate { get; set; }
        public string AircraftType { get; set; }
        public List<CabinMap> Cabins { get; set; } = new();
    }
}