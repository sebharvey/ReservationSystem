namespace Res.Domain.Dto
{
    public class PaymentCaptureResult
    {
        public bool Success { get; set; }
        public string CaptureCode { get; set; }
        public string TransactionId { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public DateTime CaptureDateTime { get; set; }
        public decimal CapturedAmount { get; set; }
        public string Currency { get; set; }
    }
}