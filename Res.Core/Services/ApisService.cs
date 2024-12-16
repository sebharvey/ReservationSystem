using System.Globalization;
using Res.Core.Interfaces;
using Res.Domain.Entities.CheckIn;

namespace Res.Core.Services
{
    public class ApisService : IApisService
    {
        private readonly IReservationService _reservationService;
        private readonly HttpClient _httpClient;

        public ApisService(IReservationService reservationService, HttpClient httpClient)
        {
            _reservationService = reservationService;
            _httpClient = httpClient;
        }

        public async Task<ApisData> BuildApisFromPnr(string recordLocator, string flightNumber)
        {
            var pnr = await _reservationService.RetrievePnr(recordLocator);
            if (pnr == null)
                throw new Exception("PNR NOT FOUND");

            var segment = pnr.Segments.FirstOrDefault(s => s.FlightNumber == flightNumber);
            if (segment == null)
                throw new Exception("FLIGHT NOT FOUND IN PNR");

            var apisData = new ApisData
            {
                RecordLocator = recordLocator,
                FlightNumber = flightNumber,
                DepartureDate = segment.DepartureDate,
                Passengers = new List<PassengerApis>()
            };

            foreach (var passenger in pnr.Passengers)
            {
                // Find DOCS SSR for this passenge
                var docsSsr = pnr.SpecialServiceRequests.FirstOrDefault(ssr =>
                    ssr.Code == "DOCS" &&
                    ssr.PassengerId == passenger.PassengerId);

                if (docsSsr == null)
                    throw new Exception($"NO DOCS FOUND FOR PASSENGER {passenger.PassengerId}");

                // Parse DOCS SSR
                // Format: HK1/P/GBR/P12345678/GBR/12JUL82/M/20NOV25/SMITH/JOHN
                var docsParts = docsSsr.Text.Split('/');

                // Find address SSR
                var addressSsr = pnr.SpecialServiceRequests.FirstOrDefault(ssr =>
                    ssr.Code == "DOCA" &&
                    ssr.PassengerId == passenger.PassengerId);

                if (addressSsr == null)
                    throw new Exception($"NO ADDRESS FOUND FOR PASSENGER {passenger.PassengerId}");

                // Parse address SSR
                // Format: HK1/R/GBR/123 HIGH STREET/LONDON/GB/W1A 1AA
                var addressParts = addressSsr.Text.Split('/');

                var passengerApis = new PassengerApis
                {
                    PassengerId = passenger.PassengerId,
                    DocumentType = docsParts[1],
                    DocumentNumber = docsParts[3],
                    DocumentIssuingCountry = docsParts[2],
                    DocumentExpiryDate = DateTime.ParseExact(docsParts[7], "ddMMMyyyy", CultureInfo.InvariantCulture),
                    LastName = docsParts[8],
                    FirstName = docsParts[9],
                    Nationality = docsParts[4],
                    DateOfBirth = DateTime.ParseExact(docsParts[5], "ddMMMyyyy", CultureInfo.InvariantCulture),
                    Gender = docsParts[6],
                    CountryOfResidence = addressParts[2],
                    ResidenceAddress = addressParts[3],
                    ResidenceCity = addressParts[4],
                    ResidencePostalCode = addressParts[6]
                };

                apisData.Passengers.Add(passengerApis);
            }

            return apisData;
        }

        public async Task<bool> ValidateApisData(ApisData apisData)
        {
            foreach (var passenger in apisData.Passengers)
            {
                // Document validations
                if (string.IsNullOrEmpty(passenger.DocumentNumber))
                    throw new Exception($"INVALID DOCUMENT NUMBER - PAX {passenger.PassengerId}");

                if (passenger.DocumentExpiryDate <= DateTime.Now)
                    throw new Exception($"EXPIRED DOCUMENT - PAX {passenger.PassengerId}");

                // Check minimum passport validity (usually 6 months)
                if (passenger.DocumentExpiryDate <= DateTime.Now.AddMonths(6))
                    throw new Exception($"PASSPORT EXPIRES WITHIN 6 MONTHS - PAX {passenger.PassengerId}");

                // Personal information validations
                if (string.IsNullOrEmpty(passenger.LastName?.Trim()) || string.IsNullOrEmpty(passenger.FirstName?.Trim()))
                    throw new Exception($"INVALID NAME - PAX {passenger.PassengerId}");

                if (passenger.DateOfBirth == default)
                    throw new Exception($"INVALID DATE OF BIRTH - PAX {passenger.PassengerId}");

                // Address validations
                if (string.IsNullOrEmpty(passenger.ResidenceAddress) ||
                    string.IsNullOrEmpty(passenger.ResidenceCity) ||
                    string.IsNullOrEmpty(passenger.ResidencePostalCode))
                    throw new Exception($"INCOMPLETE ADDRESS - PAX {passenger.PassengerId}");
            }

            return true;
        }

        public async Task<bool> SubmitApisData(ApisData apisData)
        {
            try
            {
                // Example endpoints for different countries:
                var endpoint = apisData.Passengers[0].DocumentIssuingCountry switch
                {
                    "USA" => "https://apis.cbp.gov/submitapis", // US Customs and Border Protection
                    "GBR" => "https://apis.ukba.gov.uk/submitapis", // UK Border Agency
                    "AUS" => "https://apis.abf.gov.au/submitapis", // Australian Border Force
                    _ => throw new Exception("UNSUPPORTED DESTINATION COUNTRY")
                };

                // TODO uncomment this to actually send the data
                //var response = await _httpClient.PostAsJsonAsync(endpoint, apisData);

                //if (!response.IsSuccessStatusCode)
                //    throw new Exception($"APIS SUBMISSION FAILED: {response.StatusCode}");

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"APIS SUBMISSION ERROR: {ex.Message}");
            }
        }
    }
}