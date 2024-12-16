using Res.Domain.Entities.CheckIn;
using Res.Domain.Entities.Pnr;

namespace Res.Core.Interfaces
{
    public interface ICheckInService
    {
        Task<BoardingPass> CheckIn(string recordLocator, CheckInRequest request);
        Task<List<BoardingPass>> CheckInAll(string recordLocator, string flightNumber);
        Task<bool> CancelCheckIn(string recordLocator, int passengerId, string flightNumber);
        Task<bool> ValidateCheckinWindow(Segment segment);
        Task<Pnr> ValidatePnr(string requestRecordLocator, string requestFrom);
    }
}