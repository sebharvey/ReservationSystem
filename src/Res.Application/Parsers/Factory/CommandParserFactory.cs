using Res.Domain.Entities.CheckIn;
using Res.Domain.Requests;

namespace Res.Application.Parsers.Factory
{
    public class CommandParserFactory : ICommandParserFactory
    {
        private readonly Dictionary<Type, object> _parsers;

        public CommandParserFactory()
        {
            _parsers = new Dictionary<Type, object>
            {
                { typeof(PricePnrRequest), new PricePnrCommandParser() },
                { typeof(StoreFareRequest), new StoreFareCommandParser() },
                // Add other parsers here as needed


                // New parsers
                { typeof(AddNameRequest), new AddNameCommandParser() },
                { typeof(AvailabilityRequest), new AvailabilityCommandParser() },
                { typeof(SellSegmentRequest), new SellSegmentCommandParser() },
                { typeof(SsrRequest), new SsrCommandParser() },
                { typeof(TicketingRequest), new TicketingCommandParser() },
                { typeof(ContactRequest), new ContactCommandParser() },
                { typeof(AgencyRequest), new AgencyCommandParser() },
                { typeof(SeatAssignmentRequest), new SeatAssignmentCommandParser() },
                { typeof(FormOfPaymentRequest), new FormOfPaymentCommandParser() },
                { typeof(CheckInRequest), new CheckInCommandParser() },
                { typeof(DocumentRequest), new DocumentCommandParser() },
                { typeof(RemarkRequest), new RemarkCommandParser() }
            };
        }

        public ICommandParser<T> GetParser<T>() where T : class
        {
            if (_parsers.TryGetValue(typeof(T), out var parser))
            {
                return (ICommandParser<T>)parser;
            }

            throw new ArgumentException($"No parser found for type {typeof(T).Name}");
        }
    }
}
