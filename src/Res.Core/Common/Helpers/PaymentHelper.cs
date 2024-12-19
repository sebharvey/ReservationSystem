namespace Res.Core.Common.Helpers
{
    public static class PaymentHelper
    {
        public static string MaskCreditCard(string cardType, string cardNumber, string expiry)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
                return "INVALID CARD";

            return $"CC {cardType} ****{cardNumber.Substring(cardNumber.Length - 4)}/{expiry}";
        }
    }
}