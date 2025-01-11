namespace Res.Microservices.Fares.Application.DTOs
{
    public class PassengerPricingDto
    {
        public string PassengerType { get; set; }
        public int Quantity { get; set; }
        public decimal BaseFare { get; set; }
        public decimal Taxes { get; set; }
        public decimal Total { get; set; }
    }
}