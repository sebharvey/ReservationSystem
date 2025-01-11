namespace Res.Microservices.Fares.Application.DTOs
{
    public class CabinPricingDto
    {
        public List<PassengerPricingDto> Pricing { get; set; }
        public decimal TotalItinerary { get; set; }
        public CabinDetailsDto CabinDetails { get; set; }
        public PricingFactorsDto Factors { get; set; }
        public string Explanation { get; set; }
    }
}