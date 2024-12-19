namespace Res.Domain.Entities.CheckIn
{
    public class CheckInRequest
    {
        public string RecordLocator { get; set; }
        public int PassengerId { get; set; }
        public string FlightNumber { get; set; }
        public string SeatNumber { get; set; }
        public bool HasCheckedBags { get; set; }
        public decimal BaggageWeight { get; set; }
        public int BaggageCount { get; set; }
        public List<Document> TravelDocuments { get; set; } = new();
        public Dictionary<string, string> ApiData { get; set; } = new();
        public List<string> SsrCodes { get; set; } = new();
    }
}