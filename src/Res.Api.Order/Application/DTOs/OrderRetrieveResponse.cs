namespace Res.Api.Order.Application.DTOs
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

        public class BoardingPass
        {
            public int PassengerId { get; set; }
            public string TicketNumber { get; set; }
            public string FlightNumber { get; set; }
            public string Origin { get; set; }
            public string Destination { get; set; }
            public string DepartureDate { get; set; }
            public string DepartureTime { get; set; }
            public string DepartureGate { get; set; }
            public string SeatNumber { get; set; }
            public string BoardingGroup { get; set; }
            public string Sequence { get; set; }
            public bool HasCheckedBags { get; set; }
            public decimal BaggageWeight { get; set; }
            public int BaggageCount { get; set; }
            public CheckInStatus Status { get; set; }
            public DateTime CheckInTime { get; set; }
            public string BarcodeData { get; set; }
            public string FastTrack { get; set; }
            public string LoungeAccess { get; set; }
            public List<string> SecurityMessages { get; set; } = new();
            public List<string> RegulatoryMessages { get; set; } = new();
            public Passenger Passenger { get; set; }
            public string BookingClass { get; set; }
        }

        public class Passenger
        {
            public int PassengerId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Title { get; set; }
            public PassengerType Type { get; set; }
            public List<Document> Documents { get; set; }
        }

        public class Document
        {
            public string Type { get; set; }  // PP (Passport), ID (Identity Card), VS (Visa)
            public string Number { get; set; }
            public string IssuingCountry { get; set; }
            public DateTime ExpiryDate { get; set; }
            public DateTime? IssueDate { get; set; }
            public string Nationality { get; set; }
            public DateTime DateOfBirth { get; set; }
            public string Gender { get; set; }
            public string StatusCode { get; set; }
            public string Surname { get; set; }
            public string Firstname { get; set; }
        }

        public enum PassengerType
        {
            Adult,        // ADT
            Child,        // CHD
            Infant,       // INF
            Student,      // STU
            Senior,       // SRC
            Military      // MIL
        }

        public enum CheckInStatus
        {
            NotCheckedIn,
            CheckedIn,
            BoardingPassPrinted,
            Boarded,
            NoShow,
            Denied
        }

        public enum TicketStatus
        {
            Valid,        // OK to fly
            Used,         // Already flown
            Voided,       // Cancelled before use
            Refunded,     // Money returned
            Suspended     // Temporarily invalid
        }

        public enum CouponStatus
        {
            Open,           // O - Available for use
            Used,           // F - Flight has been flown
            Exchanged,      // E - Exchanged for new ticket
            Refunded,       // R - Money returned to passenger
            Void,           // V - Cancelled before any use 
            Suspended,      // S - Temporarily suspended
            AirportControl, // A - Under airport control
            CheckedIn,      // C - Passenger checked in
            Lifted,         // L - Boarding pass issued
            IrregularOps,   // I - Flight disruption/schedule change
            Printed,        // P - Ticket printed but not used
            Conjunction,    // J - Part of a conjunction ticket
            NotValid        // N - Not valid for travel
        }

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

        public class Coupon
        {
            public string CouponNumber { get; set; } // 1, 2, 3, 4
            public string FlightNumber { get; set; }
            public string Origin { get; set; }
            public string Destination { get; set; }
            public string BookingClass { get; set; }
            public string FareBasis { get; set; }
            public decimal FareAmount { get; set; }
            public CouponStatus Status { get; set; }
            public string DepartureDate { get; set; }
        }

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