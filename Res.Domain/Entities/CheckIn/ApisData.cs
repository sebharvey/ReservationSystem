namespace Res.Domain.Entities.CheckIn
{
    public class ApisData
    {
        public string RecordLocator { get; set; }
        public string FlightNumber { get; set; }
        public string DepartureDate { get; set; }
        public List<PassengerApis> Passengers { get; set; } = new();
    }
}