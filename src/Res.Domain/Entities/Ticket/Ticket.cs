using Res.Domain.Enums;

namespace Res.Domain.Entities.Ticket
{
    public class Ticket
    {
        public string TicketNumber { get; set; }
        public string PnrLocator { get; set; }
        public int PassengerId { get; set; }
        public decimal FareAmount { get; set; }
        public TicketStatus Status { get; set; }
        public List<Coupon> Coupons { get; set; } = new();
        public DateTime IssueDate { get; set; }
        public string IssuingOffice { get; set; }
        public string ValidatingCarrier { get; set; }
    }
}