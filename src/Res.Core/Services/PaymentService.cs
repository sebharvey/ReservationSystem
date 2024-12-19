using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Res.Core.Interfaces;
using Res.Domain.Dto;
using Res.Domain.Exceptions;

namespace Res.Core.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _merchantId;
        private readonly string _apiKey;

        public PaymentService(ILogger<PaymentService> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _merchantId = configuration["Payment:MerchantId"];
            _apiKey = configuration["Payment:ApiKey"];
        }

        public async Task<PaymentAuthorizationResult> Authorize(string cardType, string cardNumber, string expiryDate, decimal amount, string currency, string reference)
        {
            try
            {
                _logger.LogInformation("Initiating payment authorization for reference: {Reference}", reference);

                if (!ValidateCard(cardNumber, expiryDate))
                {
                    return new PaymentAuthorizationResult
                    {
                        Success = false,
                        ResponseCode = "INVALID_CARD",
                        ResponseMessage = "Invalid card details provided"
                    };
                }

                // TODO: In real implementation, would make API call to payment gateway
                // var request = new AuthorizationRequest
                // {
                //     MerchantId = _merchantId,
                //     CardNumber = cardNumber,
                //     ExpiryDate = expiryDate,
                //     Amount = amount,
                //     Currency = currency,
                //     Reference = reference
                // };
                // var response = await _httpClient.PostAsJsonAsync("https://payment.gateway/authorize", request);
                // response.EnsureSuccessStatusCode();

                // Simulated successful response
                return new PaymentAuthorizationResult
                {
                    Success = true,
                    AuthorizationCode = $"AUTH{DateTime.UtcNow.Ticks}",
                    TransactionId = Guid.NewGuid().ToString(),
                    ResponseCode = "APPROVED",
                    ResponseMessage = "Authorization approved",
                    AuthorizationDateTime = DateTime.UtcNow,
                    AuthorizedAmount = amount,
                    Currency = currency,
                    LastFourDigits = cardNumber.Substring(cardNumber.Length - 4),
                    CardType = cardType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment authorization failed for reference: {Reference}", reference);
                throw new PaymentProcessingException("Payment authorization failed", ex);
            }
        }

        public async Task<PaymentCaptureResult> Capture(string authorizationCode, decimal amount, string currency, string reference)
        {
            try
            {
                _logger.LogInformation("Initiating payment capture for auth code: {AuthCode}", authorizationCode);

                // TODO: In real implementation, would make API call to payment gateway
                // var request = new CaptureRequest
                // {
                //     MerchantId = _merchantId,
                //     AuthorizationCode = authorizationCode,
                //     Amount = amount,
                //     Currency = currency,
                //     Reference = reference
                // };
                // var response = await _httpClient.PostAsJsonAsync("https://payment.gateway/capture", request);
                // response.EnsureSuccessStatusCode();

                return new PaymentCaptureResult
                {
                    Success = true,
                    CaptureCode = $"CAP{DateTime.UtcNow.Ticks}",
                    TransactionId = Guid.NewGuid().ToString(),
                    ResponseCode = "CAPTURED",
                    ResponseMessage = "Payment captured successfully",
                    CaptureDateTime = DateTime.UtcNow,
                    CapturedAmount = amount,
                    Currency = currency
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment capture failed for auth code: {AuthCode}", authorizationCode);
                throw new PaymentProcessingException("Payment capture failed", ex);
            }
        }

        public async Task<bool> ReverseAuthorization(string authorizationCode, string reference)
        {
            try
            {
                _logger.LogInformation("Reversing authorization: {AuthCode}", authorizationCode);

                // TODO: In real implementation, would make API call to payment gateway
                // var request = new ReverseAuthRequest
                // {
                //     MerchantId = _merchantId,
                //     AuthorizationCode = authorizationCode,
                //     Reference = reference
                // };
                // var response = await _httpClient.PostAsJsonAsync("https://payment.gateway/reverse", request);
                // response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authorization reversal failed for auth code: {AuthCode}", authorizationCode);
                throw new PaymentProcessingException("Authorization reversal failed", ex);
            }
        }

        public async Task<PaymentRefundResult> Refund(string transactionId, decimal amount, string currency, string reference)
        {
            try
            {
                _logger.LogInformation("Initiating refund for transaction: {TransactionId}", transactionId);

                // TODO: In real implementation, would make API call to payment gateway
                // var request = new RefundRequest
                // {
                //     MerchantId = _merchantId,
                //     TransactionId = transactionId,
                //     Amount = amount,
                //     Currency = currency,
                //     Reference = reference
                // };
                // var response = await _httpClient.PostAsJsonAsync("https://payment.gateway/refund", request);
                // response.EnsureSuccessStatusCode();

                return new PaymentRefundResult
                {
                    Success = true,
                    RefundCode = $"REF{DateTime.UtcNow.Ticks}",
                    TransactionId = Guid.NewGuid().ToString(),
                    ResponseCode = "REFUNDED",
                    ResponseMessage = "Refund processed successfully",
                    RefundDateTime = DateTime.UtcNow,
                    RefundedAmount = amount,
                    Currency = currency
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund failed for transaction: {TransactionId}", transactionId);
                throw new PaymentProcessingException("Refund processing failed", ex);
            }
        }

        public bool ValidateCard(string cardNumber, string expiryDate)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(cardNumber) || string.IsNullOrWhiteSpace(expiryDate))
                return false;

            // Remove spaces and hyphens
            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            // Check card number length and starting digits
            bool isValidCardFormat = cardNumber switch
            {
                var n when n.StartsWith("4") && (n.Length == 13 || n.Length == 16) => true, // Visa
                var n when (n.StartsWith("51") || n.StartsWith("52") || n.StartsWith("53") ||
                            n.StartsWith("54") || n.StartsWith("55")) && n.Length == 16 => true, // Mastercard
                var n when (n.StartsWith("34") || n.StartsWith("37")) && n.Length == 15 => true, // Amex
                _ => false
            };

            if (!isValidCardFormat)
                return false;

            // Check expiry format and date
            if (expiryDate.Length != 4)
                return false;

            int month = int.Parse(expiryDate.Substring(0, 2));
            int year = int.Parse("20" + expiryDate.Substring(2, 2));
            var cardExpiry = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

            if (cardExpiry <= DateTime.Today)
                return false;

            return IsValidLuhn(cardNumber);
        }

        private bool IsValidLuhn(string number)
        {
            // Remove any spaces or hyphens
            number = number.Replace(" ", "").Replace("-", "");

            int sum = 0;
            bool alternate = false;

            // Process digits from right to left
            for (int i = number.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(number[i]))
                    return false;

                int digit = int.Parse(number[i].ToString());

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }

                sum += digit;
                alternate = !alternate;
            }

            return (sum % 10 == 0);
        }
    }
}