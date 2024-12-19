using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class FormOfPaymentCommandParser : ICommandParser<FormOfPaymentRequest>
    {
        // todo Used for form of payment - Needs implementing in ProcessFormOfPayment
        public FormOfPaymentRequest Parse(string command)
        {
            // Formats: 
            // FP*CC/VISA/4444333322221111/0625/GBP892.00
            // FP*CA/GBP892.00
            // FP*MS/INVOICE/GBP892.00
            try
            {
                string fop = command.Substring(3);
                var parts = fop.Split('/');

                var request = new FormOfPaymentRequest
                {
                    Type = parts[0]
                };

                switch (parts[0])
                {
                    case "CC":
                        if (parts.Length != 5)
                            throw new ArgumentException();
                        request.CardType = parts[1];
                        request.CardNumber = parts[2];
                        request.ExpiryDate = parts[3];
                        request.Amount = parts[4];
                        break;

                    case "CA":
                        if (parts.Length != 2)
                            throw new ArgumentException();
                        request.Amount = parts[1];
                        break;

                    case "MS":
                        if (parts.Length != 3)
                            throw new ArgumentException();
                        request.Reference = parts[1];
                        request.Amount = parts[2];
                        break;

                    default:
                        throw new ArgumentException("Invalid payment type");
                }

                return request;
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID FORM OF PAYMENT FORMAT");
            }
        }
    }
}