using Res.Api.Order.Application.DTOs;

namespace Res.Api.Order.Application.Interfaces
{
    public interface ISeatService
    {
        Task<SeatMapResponse> ViewSeatMap(SeatMapRequest seatMapRequest);
    }
}
