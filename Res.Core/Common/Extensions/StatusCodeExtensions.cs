using Res.Domain.Enums;

namespace Res.Core.Common.Extensions
{
    public static class StatusCodeExtensions
    {
        public static string ToStatusCode(this SegmentStatus status)
        {
            return status switch
            {
                SegmentStatus.Holding => "NN",
                SegmentStatus.Confirmed => "HK",
                SegmentStatus.Waitlisted => "HL",
                SegmentStatus.RequestPending => "NN",
                SegmentStatus.Cancelled => "XX",
                SegmentStatus.Flown => "TK",
                _ => "UC"  // Unable to confirm as default
            };
        }

        public static string ToCouponStatusCode(this CouponStatus status)
        {
            return status switch
            {
                CouponStatus.Open => "O",
                CouponStatus.Used => "F",
                CouponStatus.Exchanged => "E",
                CouponStatus.Refunded => "R",
                CouponStatus.Void => "V",
                CouponStatus.Suspended => "S",
                CouponStatus.AirportControl => "A",
                CouponStatus.CheckedIn => "C",
                CouponStatus.Lifted => "L",
                CouponStatus.IrregularOps => "I",
                CouponStatus.Printed => "P",
                CouponStatus.Conjunction => "J",
                CouponStatus.NotValid => "N",
                _ => "?"
            };
        }
    }
}