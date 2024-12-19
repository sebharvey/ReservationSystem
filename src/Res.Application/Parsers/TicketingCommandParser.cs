using System.Globalization;
using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class TicketingCommandParser : ICommandParser<TicketingRequest>
    {
        // todo Used for ticketing time limit - Needs implementing in ProcessAddTicketingArrangement
        public TicketingRequest Parse(string command)
        {
            // Format: TLTL10DEC/BA
            try
            {
                var parts = command.Substring(4).Split('/');

                return new TicketingRequest
                {
                    TimeLimit = DateTime.ParseExact(parts[0], "ddMMM",
                        CultureInfo.InvariantCulture),
                    ValidatingCarrier = parts.Length > 1 ? parts[1] : null
                };
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID TICKETING FORMAT - USE TLTL10DEC/BA");
            }
        }
    }
}