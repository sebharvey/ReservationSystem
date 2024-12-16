using Res.Domain.Enums;
using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class StoreFareCommandParser : ICommandParser<StoreFareRequest>
    {
        public StoreFareRequest Parse(string command)
        {
            var request = new StoreFareRequest();

            // Handle basic FS command (default to option 1 for all pax types)
            if (command == "FS")
            {
                request.IsDefaultSelection = true;
                return request;
            }

            // Remove "FS" from the command
            string options = command.Substring(2);

            // Handle single option for all passengers (e.g., "FS1")
            if (int.TryParse(options, out int singleOption))
            {
                request.FareSelections[PassengerType.Adult] = singleOption;
                request.FareSelections[PassengerType.Child] = singleOption;
                request.FareSelections[PassengerType.Infant] = singleOption;
                return request;
            }

            // Handle multiple options (e.g., "FS1,2,3")
            var selections = options.Split(',');
            var passengerTypes = new[] { PassengerType.Adult, PassengerType.Child, PassengerType.Infant };

            for (int i = 0; i < selections.Length && i < passengerTypes.Length; i++)
            {
                if (int.TryParse(selections[i], out int option))
                {
                    request.FareSelections[passengerTypes[i]] = option;
                }
                else
                {
                    throw new ArgumentException($"Invalid fare option: {selections[i]}");
                }
            }

            if (!request.FareSelections.Any())
            {
                throw new ArgumentException("Invalid fare selection format");
            }

            return request;
        }
    }
}
