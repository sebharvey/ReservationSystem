namespace Res.Domain.Entities.Inventory
{
    public class FlightStatus
    {
        public string FlightNumber { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Aircraft { get; set; }
        public DateTime DepartureDateTime { get; set; }
        public DateTime ArrivalDateTime { get; set; }
    }
}
