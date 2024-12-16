using Res.Domain.Enums;

namespace Res.Domain.Entities.Ticket
{
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
}