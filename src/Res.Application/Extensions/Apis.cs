using System.Text;
using Res.Domain.Entities.CheckIn;

namespace Res.Application.Extensions
{
    public static class Apis
    {
        public static string OutputApisData(this ApisData apisData)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"APIS DATA FOR {apisData.FlightNumber} {apisData.DepartureDate}");
            sb.AppendLine(new string('-', 75));

            foreach (var pax in apisData.Passengers)
            {
                sb.AppendLine($"PAX {pax.PassengerId}: {pax.LastName}/{pax.FirstName}");
                sb.AppendLine($"DOC: {pax.DocumentType} {pax.DocumentNumber} {pax.DocumentIssuingCountry}");
                sb.AppendLine($"EXP: {pax.DocumentExpiryDate:ddMMMyy}");
                sb.AppendLine($"NAT: {pax.Nationality} DOB: {pax.DateOfBirth:ddMMMyy} SEX: {pax.Gender}");
                sb.AppendLine($"ADR: {pax.ResidenceAddress}");
                sb.AppendLine($"     {pax.ResidenceCity}, {pax.ResidenceState} {pax.ResidencePostalCode}");
                sb.AppendLine($"     {pax.CountryOfResidence}");
                sb.AppendLine(new string('-', 75));
            }

            return sb.ToString();
        }
    }
}