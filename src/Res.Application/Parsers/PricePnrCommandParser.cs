using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class PricePnrCommandParser : ICommandParser<PricePnrRequest>
    {
        public PricePnrRequest Parse(string command)
        {
            var request = new PricePnrRequest();

            if (string.IsNullOrWhiteSpace(command))
                return request;

            // Parse command options
            if (command.Contains(","))
            {
                var options = command.Split(',');
                foreach (var option in options)
                {
                    ParseOption(option.Trim(), request);
                }
            }
            else if (command.Length > 3) // Single option without comma
            {
                ParseOption(command, request);
            }

            return request;
        }

        private void ParseOption(string option, PricePnrRequest request)
        {
            switch (option)
            {
                case "R":
                    request.IsReprice = true;
                    break;

                case var o when o.StartsWith("FC-"):
                    ParseCurrency(o, request);
                    break;

                // Add more option parsers here
                // Example:
                // case var o when o.StartsWith("PT-"):
                //     ParsePassengerType(o, request);
                //     break;
            }
        }

        private void ParseCurrency(string option, PricePnrRequest request)
        {
            string currency = option.Substring(3);
            if (currency.Length != 3)
                throw new ArgumentException("INVALID CURRENCY FORMAT - USE FXP/R,FC-GBP");

            request.Currency = currency;
        }
    }
}
