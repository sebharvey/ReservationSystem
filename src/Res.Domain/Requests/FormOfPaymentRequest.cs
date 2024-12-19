namespace Res.Domain.Requests
{
    public class FormOfPaymentRequest
    {
        public string Type { get; set; }  // CC, CA, MS
        public string CardType { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string Amount { get; set; }
        public string Reference { get; set; }
    }
}