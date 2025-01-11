namespace Res.Microservices.Fares.Application.DTOs
{
    public class PricingFactorsDto
    {
        public string Seasonality { get; set; }
        public string DemandLevel { get; set; }
        public string CompetitionLevel { get; set; }
        public int DaysUntilDeparture { get; set; }
        public double CabinLoadFactor { get; set; }
        public string TimeOfDay { get; set; }
    }
}