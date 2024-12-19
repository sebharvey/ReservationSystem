using Res.Api.Models;
using Res.Api.Models.FlightBooking.Models;

namespace Res.Api.Core.Services
{
    public interface IOrderService
    {
        Task<OrderCreateResponse> Create(OrderCreateRequest orderCreateRequest);
        Task<OrderRetrieveResponse> Retrieve(OrderRetrieveRequest request);
    }
}