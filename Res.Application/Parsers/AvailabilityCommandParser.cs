using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class AvailabilityCommandParser : ICommandParser<AvailabilityRequest>
    {
        // todo Used for availability search - Needs implementing in ProcessAvailability
        public AvailabilityRequest Parse(string command)
        {
            // Format: AN20JUNLHRJFK/1400
            if (command.Length < 13)
                throw new ArgumentException("Invalid availability format");

            try
            {
                return new AvailabilityRequest
                {
                    DepartureDate = command.Substring(2, 5),
                    Origin = command.Substring(7, 3),
                    Destination = command.Substring(10, 3),
                    PreferredTime = command.Length > 13 && command[13] == '/'
                        ? command.Substring(14, 4)
                        : null
                };
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID AVAILABILITY FORMAT - USE AN20JUNLHRJFK/1400");
            }
        }
    }
}