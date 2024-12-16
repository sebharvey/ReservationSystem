using Res.Core.Common.Extensions;
using Res.Domain.Entities.Pnr;

namespace Res.Application.Extensions
{
    public static class SegmentExtensions
    {
        public static string OutputAddSegment(this Segment segment)
        {
            if (segment == null)
                return "NO FLIGHTS FOUND";

            if (segment.IsSurfaceSegment)
            {
                return "ARNK";
            }

            return "ADDED " +
                   $"{segment.FlightNumber} {segment.BookingClass} " +
                   $"{segment.Origin}{segment.Destination} " +
                   $"{segment.Status.ToStatusCode()}{segment.Quantity} " +
                   $"{segment.DepartureDate} {segment.DepartureTime} " +
                   $"{segment.ArrivalTime}";

        }
    }
}