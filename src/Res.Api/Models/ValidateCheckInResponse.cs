namespace Res.Api.Models
{
    public class ValidateCheckInResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string RecordLocator { get; set; }
        public string FlightNumber { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string DepartureDate { get; set; }
        public string DepartureTime { get; set; }
        public DateTime CheckInOpenTime { get; set; }
        public DateTime CheckInCloseTime { get; set; }
        public List<PassengerInfo> Passengers { get; set; } = new();

        public class PassengerInfo
        {
            public int PassengerId { get; set; }
            public string Title { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Type { get; set; }
            public string Ticket { get; set; }
        }
    }
}
