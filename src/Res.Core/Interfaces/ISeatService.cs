using Res.Domain.Entities.Pnr;
using Res.Domain.Entities.SeatMap;

namespace Res.Core.Interfaces
{
    public interface ISeatService
    {
        Task<SeatMap> DisplaySeatMap(string flightNumber, string departureDate, string bookingClass = null);
        Task<bool> AssignSeat(string seatNumber, int passengerId, string segmentNumber);
        Task<bool> RemoveSeat(int passengerId, string segmentNumber);
        Pnr Pnr { get; set; }
    }
}