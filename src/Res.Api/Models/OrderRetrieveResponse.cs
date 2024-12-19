using Res.Domain.Entities.CheckIn;
using Res.Domain.Entities.Ticket;

namespace Res.Api.Models
{
    public class OrderRetrieveResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string RecordLocator { get; set; }
        public string Status { get; set; }
        public List<PassengerInfo> Passengers { get; set; } = new();
        public List<SegmentInfo> Segments { get; set; } = new();
        public ContactInfo Contact { get; set; }
        public List<Ticket> Tickets { get; set; }
        public List<BoardingPass> BoardingPasses { get; set; }

        public class PassengerInfo
        {
            public string Title { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string TicketNumber { get; set; }
        }

        public class SegmentInfo
        {
            public string FlightNumber { get; set; }
            public string Origin { get; set; }
            public string Destination { get; set; }
            public string DepartureDate { get; set; }
            public string DepartureTime { get; set; }
            public string ArrivalDate { get; set; }
            public string ArrivalTime { get; set; }
            public string BookingClass { get; set; }
            public string Status { get; set; }
        }

        public class ContactInfo
        {
            public string PhoneNumber { get; set; }
            public string EmailAddress { get; set; }
        }
    }
}