namespace Res.Domain.Requests
{
    public class TicketingRequest
    {
        public DateTime TimeLimit { get; set; }
        public string ValidatingCarrier { get; set; }
    }
}