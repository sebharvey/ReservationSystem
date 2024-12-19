using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class SsrCommandParser : ICommandParser<SsrRequest>
    {
        // todo Used for SSR entries - Needs implementing in ProcessAddSsr
        public SsrRequest Parse(string command)
        {
            // Format: SR WCHR/P1/S1/TXT
            try
            {
                string[] parts = command.Substring(3).Split('/');

                return new SsrRequest
                {
                    Code = parts[0].Trim(),
                    PassengerId = parts.Length > 1 ? int.Parse(parts[1].TrimStart('P')) : 0,
                    SegmentNumber = parts.Length > 2 ? int.Parse(parts[2].TrimStart('S')) : 0,
                    Text = parts.Length > 3 ? parts[3] : null
                };
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID SSR FORMAT - USE SR CODE/P1/S1/TEXT");
            }
        }
    }
}