namespace Res.Domain.Dto
{
    public class PaymentAuthorizationResult
    {
        public bool Success { get; set; }
        public string AuthorizationCode { get; set; }
        public string TransactionId { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public DateTime AuthorizationDateTime { get; set; }
        public decimal AuthorizedAmount { get; set; }
        public string Currency { get; set; }
        public string LastFourDigits { get; set; }
        public string CardType { get; set; }
    }
}