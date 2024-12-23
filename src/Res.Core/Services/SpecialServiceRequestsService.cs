using Res.Core.Interfaces;
using Res.Domain.Entities.Pnr;
using Res.Domain.Enums;

namespace Res.Core.Services
{
    public class SpecialServiceRequestsService : ISpecialServiceRequestsService
    {
        public Pnr Pnr { get; set; }

        private readonly Dictionary<string, SsrType> _ssrCodeMap = new()
        {
            // Wheelchair codes
            { "WCHR", SsrType.Wheelchair }, // Wheelchair - R for Ramp
            { "WCHS", SsrType.Wheelchair }, // Wheelchair - S for Steps
            { "WCHC", SsrType.Wheelchair }, // Wheelchair - C for Cabin Seat

            // Meal codes
            { "VGML", SsrType.MealPreference }, // Vegetarian
            { "AVML", SsrType.MealPreference }, // Asian Vegetarian
            { "HNML", SsrType.MealPreference }, // Hindu
            { "KSML", SsrType.MealPreference }, // Kosher
            { "MOML", SsrType.MealPreference }, // Muslim
            { "NLML", SsrType.MealPreference }, // Low Lactose
            { "DBML", SsrType.MealPreference }, // Diabetic
            { "SPML", SsrType.MealPreference }, // Special Meal
            { "GFML", SsrType.MealPreference }, // Gluten Free

            // Medical
            { "MEDA", SsrType.MedicalAssistance }, // Medical Case
            { "OXYG", SsrType.MedicalAssistance }, // Oxygen Required
            { "DEAF", SsrType.MedicalAssistance }, // Deaf Passenger
            { "BLND", SsrType.MedicalAssistance }, // Blind Passenger

            // Other common codes
            { "UMNR", SsrType.UnaccompaniedMinor }, // Unaccompanied Minor
            { "MAAS", SsrType.MeetAndAssist }, // Meet and Assist
            { "EXST", SsrType.ExtraSeat }, // Extra Seat
            { "CBBG", SsrType.ExtraBaggage }, // Cabin Baggage
            { "BULK", SsrType.ExtraBaggage }, // Bulky Baggage
            { "BIKE", SsrType.SportingEquipment }, // Bicycle
            { "GOLF", SsrType.SportingEquipment }, // Golf Equipment
            { "SKI", SsrType.SportingEquipment }, // Ski Equipment

            // Animals
            { "PETC", SsrType.ServiceAnimal }, // Pet in Cabin
            { "SVAN", SsrType.ServiceAnimal }, // Service Animal
            { "ESAN", SsrType.EmotionalSupport }, // Emotional Support Animal

            // Travel Documents
            { "DOCS", SsrType.Passport }, // Passport Info
            { "DOCA", SsrType.Visa }, // Advance Passenger Info

            // Loyalty
            { "FQTV", SsrType.FrequentFlyer }, // Frequent Flyer

            // Corporate
            { "GRPF", SsrType.GroupHandling }, // Group Fare
            { "CORP", SsrType.CorporateHandling } // Corporate Booking
        };

        public async Task<bool> AddSsr(string code, int passengerId, int segmentNumber, string? text)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("SSR code is required");

            // Validate SSR code exists using the mapping
            if (!_ssrCodeMap.TryGetValue(code, out SsrType ssrType))
                throw new ArgumentException($"Invalid SSR code: {code}");

            // Validate passenger if specified
            if (!Pnr.Data.Passengers.Any(p => Convert.ToInt32(p.PassengerId) == passengerId))
                throw new ArgumentException($"Invalid passenger ID: {passengerId}");

            // Validate segment if specified
            if (segmentNumber > 0 && segmentNumber > Pnr.Data.Segments.Count)
                throw new ArgumentException($"Invalid segment number: {segmentNumber}");

            // Create new SSR
            var ssr = new Ssr
            {
                Id = Convert.ToInt32(Pnr.Data.SpecialServiceRequests.Count + 1),
                PnrLocator = Pnr.RecordLocator,
                PassengerId = Convert.ToInt32(passengerId),
                SegmentNumber = Convert.ToInt32(segmentNumber),
                Type = ssrType,
                Code = code,
                Text = text,
                Status = SsrStatus.Requested,
                CreatedDate = DateTime.UtcNow,
                ActionCode = "NN", // Need
                CompanyId = Pnr.Data.Segments.FirstOrDefault()?.FlightNumber.Substring(0, 2) ?? "YY",
                Quantity = 1
            };

            // Auto-confirm certain SSR types
            var autoConfirmedSsrs = new[]
            {
                SsrType.MealPreference,
                SsrType.FrequentFlyer,
                SsrType.Passport,
                SsrType.Visa
            };

            if (autoConfirmedSsrs.Contains(ssrType))
            {
                ssr.IsAutoConfirmed = true;
                ssr.Status = SsrStatus.Confirmed;
                ssr.ActionCode = "HK"; // Confirmed
            }

            // Add special handling for certain SSR types
            switch (ssrType)
            {
                case SsrType.Wheelchair:

                    // Store the specific wheelchair type code
                    ssr.Equipment = code; // WCHR, WCHS, or WCHC
                    break;

                case SsrType.UnaccompaniedMinor:

                    if (string.IsNullOrEmpty(text))
                        throw new ArgumentException("Text details required for UMNR");
                    break;

                case SsrType.ServiceAnimal:
                case SsrType.EmotionalSupport:

                    if (string.IsNullOrEmpty(text))
                        throw new ArgumentException("Animal details required");
                    break;
            }

            Pnr.Data.SpecialServiceRequests.Add(ssr);

            return true;
        }

        public async Task<bool> DeleteSsr(int ssrId)
        {
            var ssr = Pnr.Data.SpecialServiceRequests.FirstOrDefault(s => s.Id == ssrId);

            if (ssr == null)
                throw new ArgumentException($"SSR not found: {ssrId}");

            Pnr.Data.SpecialServiceRequests.Remove(ssr);

            return true;
        }
    }
}