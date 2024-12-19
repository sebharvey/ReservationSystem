using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class AgencyCommandParser : ICommandParser<AgencyRequest>
    {
        public AgencyRequest Parse(string command)
        {
            try
            {
                var parts = command.Substring(3).Split('/');

                var request = new AgencyRequest();

                // Always set the first part as AgencyCode if present
                if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
                    request.AgencyCode = parts[0].Trim();

                // Set IATA if provided
                if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
                    request.IataNumber = parts[1].Trim();

                // Set AgentId if provided
                if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2]))
                    request.AgentId = parts[2].Trim();

                // Validate we have at least agency code
                if (string.IsNullOrWhiteSpace(request.AgencyCode))
                    throw new ArgumentException("AGENCY CODE IS REQUIRED");

                return request;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"INVALID RECEIVED FROM FORMAT - MINIMUM REQUIRED: RF CODE\nFULL FORMAT: RF CODE/IATA/AGENTID\n{ex.Message}");
            }
        }
    }
}