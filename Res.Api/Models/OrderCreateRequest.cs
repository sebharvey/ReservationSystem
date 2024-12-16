namespace Res.Api.Models
{
    namespace FlightBooking.Models
    {
        public class OrderCreateRequest
        {
            public string OutboundOfferId { get; set; }
            public string InboundOfferId { get; set; }
            public int PassengerCount { get; set; }
            public List<Passenger> Passengers { get; set; }
            public OrderContactDetails ContactDetails { get; set; }
            public OrderPaymentDetails PaymentDetails { get; set; }
            public List<SeatSelection> Seats { get; set; }

            public class Passenger
            {
                public string Title { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
                public DateTime DateOfBirth { get; set; }
            }

            public class OrderContactDetails
            {
                public string Email { get; set; }
                public string Phone { get; set; }
                public string LoyaltyNumber { get; set; }
            }

            public class OrderPaymentDetails
            {
                public string CardNumber { get; set; }
                public string Expiry { get; set; }
                public string CVV { get; set; }
                public string CardType { get; set; }
            }
            public class SeatSelection
            {
                public string OfferId { get; set; }
                public string Seat { get; set; }
                public int Passenger { get; set; }
            }
        }
    }
}
