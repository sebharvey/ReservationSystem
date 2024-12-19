using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class RemarkCommandParser : ICommandParser<RemarkRequest>
    {
        // todo Used for remarks - Needs implementing in AddRemark
        public RemarkRequest Parse(string command)
        {
            // Format: RM FREE TEXT
            try
            {
                return new RemarkRequest
                {
                    Text = command.Substring(3)
                };
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID REMARK FORMAT");
            }
        }
    }
}