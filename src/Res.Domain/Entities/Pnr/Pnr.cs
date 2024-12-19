using Res.Domain.Enums;

namespace Res.Domain.Entities.Pnr
{
    public class Pnr
    {
        public string RecordLocator { get; set; }
        public List<Passenger> Passengers { get; set; } = new();
        public List<Segment> Segments { get; set; } = new();
        public List<SeatAssignment> SeatAssignments { get; set; } = new();
        public List<Ssr> SpecialServiceRequests { get; set; } = new();
        public List<Osi> OtherServiceInformation { get; set; } = new();
        public PnrStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public ContactInfo Contact { get; set; } = new();
        public AgencyInfo Agency { get; set; } = new();
        public TicketingInfo TicketingInfo { get; set; } = new();
        public List<Ticket.Ticket> Tickets { get; set; } = new();
        public List<string> Remarks { get; set; } = new();
        public List<FareInfo> Fares { get; set; } = new();
        public string FormOfPayment { get; set; }
    }
}