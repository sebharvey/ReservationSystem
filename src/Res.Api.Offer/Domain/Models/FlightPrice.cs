namespace Res.Api.Offer.Domain.Models
{
    public class FlightPrice
    {
        public string FlightNumber { get; set; }
        public decimal EconomyPrice { get; set; }
        public decimal PremiumPrice { get; set; }
        public decimal BusinessPrice { get; set; }
    }
}