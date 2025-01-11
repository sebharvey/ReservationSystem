namespace Res.Microservices.Fares.Domain.Entities
{
    public class CabinInventory
    {
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public double OccupancyPercentage => ((TotalSeats - AvailableSeats) / (double)TotalSeats) * 100;
    }
}