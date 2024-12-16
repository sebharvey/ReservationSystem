using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class SeatAssignmentCommandParser : ICommandParser<SeatAssignmentRequest>
    {
        // todo Used for seat assignments - Needs implementing in ProcessAssignSeat
        public SeatAssignmentRequest Parse(string command)
        {
            // Format: ST/4D/P1/S1
            try
            {
                var parts = command.Substring(3).Split('/');
                if (parts.Length != 3)
                    throw new ArgumentException();

                return new SeatAssignmentRequest
                {
                    SeatNumber = parts[0],
                    PassengerId = int.Parse(parts[1].Substring(1)),
                    SegmentNumber = parts[2].Substring(1)
                };
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID SEAT ASSIGNMENT FORMAT - USE ST/4D/P1/S1");
            }
        }
    }
}