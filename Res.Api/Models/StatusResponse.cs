namespace Res.Api.Models
{
    public class StatusResponse
    {
        public string FlightNumber { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Aircraft { get; set; }
        public DateTime DepartureDateTime { get; set; }
        public DateTime ArrivalDateTime { get; set; }
        public FlightStatus Status { get; set; }

        public enum FlightStatus
        {
            OnTime,     // Default
            Boarding,   // Set 1hr before departure time
            Departed,   // After DepartureDateTime but before ArrivalDateTime
            Landed      // After ArrivalDateTime
        }
    }
}
