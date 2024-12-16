using Res.Domain.Dto;

namespace Res.Core.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentAuthorizationResult> Authorize(string cardType, string cardNumber, string expiryDate, decimal amount, string currency, string reference);
        Task<PaymentCaptureResult> Capture(string authorizationCode, decimal amount, string currency, string reference);
        Task<bool> ReverseAuthorization(string authorizationCode, string reference);
        Task<PaymentRefundResult> Refund(string transactionId, decimal amount, string currency, string reference);
        bool ValidateCard(string cardNumber, string expiryDate);
    }
}