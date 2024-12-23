using Res.Domain.Enums;

namespace Res.Domain.Entities.Pnr
{
    public class Pnr
    {
        public Guid Id { get; set; }
        public string RecordLocator { get; set; }
        public string JsonData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property for the deserialized data
        public PnrData Data { get; set; }
        public Guid? SessionId { get; set; }
        public DateTime? SessionTimestamp { get; set; }

        public class PnrData
        {
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
}