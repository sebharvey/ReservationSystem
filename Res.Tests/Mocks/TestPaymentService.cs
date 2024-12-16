using Res.Core.Interfaces;
using Res.Domain.Dto;

namespace Res.Tests.Mocks
{
    public class TestPaymentService : IPaymentService
    {
        public Task<PaymentAuthorizationResult> Authorize(
            string cardType, string cardNumber, string expiryDate,
            decimal amount, string currency, string reference)
        {
            return Task.FromResult(new PaymentAuthorizationResult
            {
                Success = true,
                AuthorizationCode = "TEST_AUTH",
                TransactionId = "TEST_TRANS",
                ResponseCode = "APPROVED",
                ResponseMessage = "Test Authorization",
                AuthorizationDateTime = DateTime.UtcNow,
                AuthorizedAmount = amount,
                Currency = currency,
                LastFourDigits = cardNumber.Substring(cardNumber.Length - 4),
                CardType = cardType
            });
        }

        public Task<PaymentCaptureResult> Capture(
            string authorizationCode, decimal amount,
            string currency, string reference)
        {
            return Task.FromResult(new PaymentCaptureResult
            {
                Success = true,
                CaptureCode = "TEST_CAPTURE",
                TransactionId = "TEST_TRANS",
                ResponseCode = "CAPTURED",
                ResponseMessage = "Test Capture",
                CaptureDateTime = DateTime.UtcNow,
                CapturedAmount = amount,
                Currency = currency
            });
        }

        public Task<bool> ReverseAuthorization(
            string authorizationCode, string reference)
        {
            return Task.FromResult(true);
        }

        public Task<PaymentRefundResult> Refund(
            string transactionId, decimal amount,
            string currency, string reference)
        {
            return Task.FromResult(new PaymentRefundResult
            {
                Success = true,
                RefundCode = "TEST_REFUND",
                TransactionId = "TEST_TRANS",
                ResponseCode = "REFUNDED",
                ResponseMessage = "Test Refund",
                RefundDateTime = DateTime.UtcNow,
                RefundedAmount = amount,
                Currency = currency
            });
        }

        public bool ValidateCard(string cardNumber, string expiryDate)
        {
            return true;
        }
    }
}