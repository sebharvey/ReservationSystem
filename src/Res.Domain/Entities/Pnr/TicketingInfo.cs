namespace Res.Domain.Entities.Pnr
{
    public class TicketingInfo
    {
        public DateTime TimeLimit { get; set; }
        public string ValidatingCarrier { get; set; }
        public string FareType { get; set; }
        public string Commission { get; set; }
        public string TourCode { get; set; }
        public bool IsGroupBooking { get; set; }
        public string TicketingInstructions { get; set; }
    }
}