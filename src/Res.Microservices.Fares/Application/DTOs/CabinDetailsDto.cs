namespace Res.Microservices.Fares.Application.DTOs
{
    public class CabinDetailsDto
    {
        public string CabinClass { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public double OccupancyPercentage { get; set; }
        public string DemandLevel { get; set; }
    }
}