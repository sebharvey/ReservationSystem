namespace Res.Domain.Dto
{
    public class PaymentRefundResult
    {
        public bool Success { get; set; }
        public string RefundCode { get; set; }
        public string TransactionId { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public DateTime RefundDateTime { get; set; }
        public decimal RefundedAmount { get; set; }
        public string Currency { get; set; }
    }
}