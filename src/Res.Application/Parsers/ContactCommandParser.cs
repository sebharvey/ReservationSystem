using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class ContactCommandParser : ICommandParser<ContactRequest>
    {
        // todo Used for contact details - Needs implementing in ProcessContact
        public ContactRequest Parse(string command)
        {
            // Format: CTCP 44123456789 or CTCE TEST@EMAIL.COM
            try
            {
                var type = command.Substring(3, 1); // P for phone, E for email
                var value = command.Substring(5).Trim();

                return new ContactRequest
                {
                    Type = type == "P" ? ContactType.Phone : ContactType.Email,
                    Value = value
                };
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID CONTACT FORMAT - USE CTCP PHONE or CTCE EMAIL");
            }
        }
    }
}