namespace Res.Microservices.Inventory.Application.DTOs
{
    public class SeatAllocationRequest
    {
        public DateTime DepartureDate { get; set; }
        public string FlightNo { get; set; }
        public string SeatNumber { get; set; }
        public string RecordLocator { get; set; }
        public int PaxId { get; set; }
    }
}