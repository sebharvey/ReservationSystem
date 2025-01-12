namespace Res.Microservices.Inventory.Application.DTOs
{
    public class ImportScheduleRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}