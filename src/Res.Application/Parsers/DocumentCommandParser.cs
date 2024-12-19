using System.Globalization;
using Res.Domain.Requests;

namespace Res.Application.Parsers
{
    public class DocumentCommandParser : ICommandParser<DocumentRequest>
    {
        // todo Used for travel documents - Needs implementing in ProcessAddDocument
        public DocumentRequest Parse(string command)
        {
            // Format: SRDOCS HK1/P/GBR/P12345678/GBR/12JUL82/M/20NOV25/SMITH/JOHN
            try
            {
                var parts = command.Split('/');
                if (parts.Length < 9)
                    throw new ArgumentException();

                return new DocumentRequest
                {
                    DocumentType = parts[1],
                    IssuingCountry = parts[2],
                    DocumentNumber = parts[3],
                    Nationality = parts[4],
                    DateOfBirth = DateTime.ParseExact(parts[5], "ddMMMyy",
                        CultureInfo.InvariantCulture),
                    Gender = parts[6],
                    ExpiryDate = DateTime.ParseExact(parts[7], "ddMMMyy",
                        CultureInfo.InvariantCulture),
                    LastName = parts[8],
                    FirstName = parts.Length > 9 ? parts[9] : ""
                };
            }
            catch (Exception)
            {
                throw new ArgumentException("INVALID DOCUMENT FORMAT");
            }
        }
    }
}