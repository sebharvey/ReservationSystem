namespace Res.Microservices.Fares.Domain.ValueObjects
{
    public class PricingFactors
    {
        public string Seasonality { get; set; }
        public string DemandLevel { get; set; }
        public string CompetitionLevel { get; set; }
        public int DaysUntilDeparture { get; set; }
        public double CabinLoadFactor { get; set; }
        public string TimeOfDay { get; set; }
    }
}