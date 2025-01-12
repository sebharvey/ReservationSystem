using Res.Api.Order.Application.DTOs;
using Res.Api.Order.Application.DTOs.FlightBooking.Models;

namespace Res.Api.Order.Application.Interfaces
{
    public interface IOrderService
    {
        Task<OrderCreateResponse> Create(OrderCreateRequest orderCreateRequest);
        Task<OrderRetrieveResponse> Retrieve(OrderRetrieveRequest request);
    }
}