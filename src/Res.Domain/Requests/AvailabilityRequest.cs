namespace Res.Domain.Requests
{
    public class AvailabilityRequest
    {
        public string DepartureDate { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string? PreferredTime { get; set; }
    }
}