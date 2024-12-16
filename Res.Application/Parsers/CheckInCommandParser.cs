using Res.Domain.Entities.CheckIn;

namespace Res.Application.Parsers
{
    public class CheckInCommandParser : ICommandParser<CheckInRequest>
    {
        // todo Used for check-in - Needs implementing in ProcessCheckIn
        public CheckInRequest Parse(string command)
        {
            // Format: CKIN ABC123/P1/VS001/12A/B2/20.5
            try
            {
                var parts = command.Substring(5).Split('/');
                if (parts.Length < 3)
                    throw new ArgumentException();

                var request = new CheckInRequest
                {
                    RecordLocator = parts[0],
                    PassengerId = int.Parse(parts[1].Substring(1)),
                    FlightNumber = parts[2]
                };

                // Optional parameters
                if (parts.Length > 3)
                    request.SeatNumber = parts[3];

                if (parts.Length > 4)
                {
                    request.BaggageCount = int.Parse(parts[4].TrimStart('B'));
                    request.BaggageWeight = decimal.Parse(parts[5]);
                }

                return request;
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID CHECK-IN FORMAT");
            }
        }
    }
}