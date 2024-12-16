using Res.Domain.Entities.Pnr;
using Res.Domain.Entities.SeatMap;

namespace Res.Core.Interfaces
{
    public interface ISeatService
    {
        Task<SeatMap> DisplaySeatMap(Pnr pnr, string flightNumber, string departureDate, string bookingClass = null);
        Task<bool> AssignSeat(Pnr pnr, string seatNumber, int passengerId, string segmentNumber);
        Task<bool> RemoveSeat(Pnr pnr, int passengerId, string segmentNumber);
    }
}