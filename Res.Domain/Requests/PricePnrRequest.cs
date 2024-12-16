namespace Res.Domain.Requests
{
    public class PricePnrRequest
    {
        public string Currency { get; set; } = "GBP"; // Default currency
        public bool IsReprice { get; set; }
        public Dictionary<string, object> PricingOptions { get; set; } = new();
    }
}
