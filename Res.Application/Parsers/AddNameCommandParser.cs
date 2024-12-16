using Res.Domain.Enums;
using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class AddNameCommandParser : ICommandParser<AddNameRequest>
    {
        // todo Used for name entries - Needs implementing in ProcessAddName
        public AddNameRequest Parse(string command)
        {
            // Format: NM1SMITH/JOHN MR or NM1SMITH/JOHN/CHD

            if (!command.StartsWith("NM"))
                throw new ArgumentException("Invalid name format");

            try
            {
                string nameData = command.Substring(3); // Skip NM1
                var parts = nameData.Split('/');

                if (parts.Length < 2)
                    throw new ArgumentException("INVALID NAME FORMAT - USE NM1SURNAME/FIRSTNAME TITLE");

                var request = new AddNameRequest
                {
                    LastName = parts[0],
                    FirstName = parts[1]
                };

                // Handle optional title or passenger type
                if (parts.Length > 2)
                {
                    if (Enum.TryParse<PassengerType>(parts[2], true, out var passengerType))
                    {
                        request.PassengerType = passengerType;
                    }
                    else
                    {
                        request.Title = parts[2];
                    }
                }

                return request;
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID NAME FORMAT - USE NM1SURNAME/FIRSTNAME TITLE");
            }
        }
    }
}