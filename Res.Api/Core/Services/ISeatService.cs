using Res.Api.Models;

namespace Res.Api.Core.Services
{
    public interface ISeatService
    {
        Task<SeatMapResponse> ViewSeatMap(SeatMapRequest seatMapRequest);
    }
}
