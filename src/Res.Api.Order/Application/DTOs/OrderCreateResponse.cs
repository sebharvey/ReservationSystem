namespace Res.Api.Order.Application.DTOs
{
    public class OrderCreateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string BookingConfirmation { get; set; }
    }
}
