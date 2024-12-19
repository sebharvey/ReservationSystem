using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class SellSegmentCommandParser : ICommandParser<SellSegmentRequest>
    {
        // todo Used for selling segments - Needs implementing in ProcessSellSegment
        public SellSegmentRequest Parse(string command)
        {
            // Formats: 
            // SS1Y2 (quantity/class/line number)
            // SS VS001Y24NOVLHRJFK1 (flight/class/date/origin/dest/quantity)

            try
            {
                if (command.Length > 15) // Long sell format
                {
                    return new SellSegmentRequest
                    {
                        FlightNumber = command.Substring(3, 5).Trim(),
                        BookingClass = command.Substring(8, 1),
                        DepartureDate = command.Substring(9, 5),
                        From = command.Substring(14, 3),
                        To = command.Substring(17, 3),
                        Quantity = int.Parse(command.Substring(20, 1))
                    };
                }
                else // Short sell format
                {
                    return new SellSegmentRequest
                    {
                        Quantity = int.Parse(command.Substring(2, 1)),
                        BookingClass = command.Substring(3, 1),
                        LineNumber = int.Parse(command.Substring(4))
                    };
                }
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID SELL FORMAT");
            }
        }
    }
}