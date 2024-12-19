using Res.Core.Interfaces;
using Res.Domain.Entities.Fare;
using Res.Domain.Entities.Pnr;
using Res.Domain.Requests;
using Res.Infrastructure.Interfaces;

namespace Res.Core.Services
{
    public class FareService : IFareService
    {
        private  readonly IFareRepository _fareRepository;

        private readonly Dictionary<string, decimal> _exchangeRates = new()
        {
            { "GBP", 1.0m },
            { "USD", 1.27m },
            { "EUR", 1.17m },
            { "AUD", 1.93m },
            { "CAD", 1.71m },
            { "JPY", 186.32m },
            { "CNY", 9.12m }
        };

        public FareService(IFareRepository fareRepository)
        {
            _fareRepository = fareRepository;
        }

        public async Task<Pnr> AddFare(Pnr pnr, FareInfo fare)
        {
            // Calculate fares for all fare families
            fare.AvailableFareFamilies = _fareRepository.FareFamilies;

            foreach (var fareFamily in fare.AvailableFareFamilies)
            {
                var baseFare = fare.FareAmount;
                fareFamily.BaseFare = baseFare;
                fareFamily.TotalFare = baseFare * fareFamily.PriceMultiplier;
            }

            // Add or update fare in PNR
            var existingFare = pnr.Fares.FirstOrDefault(f => f.PassengerId == fare.PassengerId);
            if (existingFare != null)
                pnr.Fares.Remove(existingFare);

            pnr.Fares.Add(fare);
            return pnr;
        }

        public async Task<Pnr> StoreFare(Pnr pnr, StoreFareRequest request)
        {
            // Group fares by passenger type
            var fareGroups = pnr.Fares
                .GroupBy(f => pnr.Passengers.First(p => p.PassengerId == f.PassengerId).Type);

            foreach (var group in fareGroups)
            {
                var passengerType = group.Key;
                int selectedOption;

                if (request.IsDefaultSelection)
                {
                    selectedOption = 1; // Default to first (cheapest) option
                }
                else if (!request.FareSelections.TryGetValue(passengerType, out selectedOption))
                {
                    throw new ArgumentException($"No fare option specified for passenger type: {passengerType}");
                }

                // Validate option exists for this passenger type
                var sampleFare = group.First();
                if (selectedOption < 1 || selectedOption > sampleFare.AvailableFareFamilies.Count)
                {
                    throw new ArgumentException($"Invalid fare option {selectedOption} for {passengerType}");
                }

                // Apply selected fare family to all passengers of this type
                foreach (var fare in group)
                {
                    var selectedFamily = fare.AvailableFareFamilies[selectedOption - 1];

                    fare.FareAmount = selectedFamily.TotalFare;
                    fare.FareFamilyCode = selectedFamily.Code;
                    fare.FareRestrictions = GenerateFareRestrictions(selectedFamily);
                    fare.IsStored = true;
                }
            }

            return pnr;
        }

        private string GenerateFareRestrictions(FareFamily fareFamily)
        {
            var restrictions = new List<string>();

            if (!fareFamily.IsRefundable)
                restrictions.Add("NONREF");

            if (!fareFamily.IsChangeable)
                restrictions.Add("NOCHNG");
            else if (fareFamily.ChangeFee > 0)
                restrictions.Add($"CHNG FEE {fareFamily.ChangeFee:C}");

            if (fareFamily.HasBaggageIncluded)
                restrictions.Add($"BAG {(fareFamily.Code == "FLEX" ? "2PC" : "1PC")}");

            return string.Join("/", restrictions);
        }

        public async Task<Pnr> AddFormOfPayment(Pnr pnr, string formOfPayment)
        {
            pnr.FormOfPayment = formOfPayment;

            return pnr;
        }

        public async Task<Pnr> PricePnr(Pnr pnr, PricePnrRequest request)
        {
            if (pnr == null)
                throw new ArgumentNullException(nameof(pnr));

            if (pnr.TicketingInfo?.TimeLimit == default)
                throw new InvalidOperationException("NO TICKETING ARRANGEMENTS - USE TLTL TO ADD");

            // Clear existing fares if repricing
            if (request.IsReprice)
            {
                pnr.Fares.Clear();
            }

            foreach (var passenger in pnr.Passengers)
            {
                var fare = CalculatePassengerFare(pnr, passenger, request.Currency);

                // Add or update fare in PNR
                var existingFare = pnr.Fares.FirstOrDefault(f => f.PassengerId == passenger.PassengerId);
                if (existingFare != null)
                {
                    pnr.Fares.Remove(existingFare);
                }
                pnr.Fares.Add(fare);
            }

            return pnr;
        }

        private FareInfo CalculatePassengerFare(Pnr pnr, Passenger passenger, string currency)
        {
            var fare = new FareInfo
            {
                PassengerId = passenger.PassengerId,
                ValidatingCarrier = pnr.TicketingInfo.ValidatingCarrier ?? "VS",
                Currency = currency,
                LastDateToTicket = pnr.TicketingInfo.TimeLimit,
                AvailableFareFamilies = new List<FareFamily>()
            };

            // Calculate base fare and fare bases
            var (baseFare, fareBases) = CalculateSegmentPrices(pnr.Segments, currency);
            fare.FareBasis = string.Join("/", fareBases);

            // Create fare family options
            foreach (var familyTemplate in _fareRepository.FareFamilies)
            {
                var family = CreateFareFamily(familyTemplate, baseFare);
                fare.AvailableFareFamilies.Add(family);
            }

            // Set the default fare amount to the basic (first) fare family
            fare.FareAmount = fare.AvailableFareFamilies.First().TotalFare;

            // Add exchange rate restriction if applicable
            if (currency != "GBP")
            {
                fare.FareRestrictions = $"ROE{_exchangeRates[currency]:F4}";
            }

            return fare;
        }

        public (decimal BaseFare, List<string> FareBases) CalculateSegmentPrices(IEnumerable<Segment> segments, string currency)
        {
            decimal baseFare = 0m;
            var fareBases = new List<string>();

            foreach (var segment in segments.Where(s => !s.IsSurfaceSegment))
            {
                var segmentFare = GetSegmentBaseFare(segment.BookingClass);
                baseFare += segmentFare;
                fareBases.Add($"{segment.BookingClass}RT{segmentFare / 100:00}");
            }

            // Apply exchange rate if not GBP
            if (currency != "GBP" && _exchangeRates.ContainsKey(currency))
            {
                baseFare *= _exchangeRates[currency];
                baseFare = Math.Round(baseFare, 2);
            }

            return (baseFare, fareBases);
        }

        private static decimal GetSegmentBaseFare(string bookingClass) => bookingClass switch
        {
            "J" => 2500.00m,
            "W" => 1200.00m,
            "Y" => 800.00m,
            _ => 500.00m
        };

        private static FareFamily CreateFareFamily(FareFamily template, decimal baseFare) => new()
        {
            Code = template.Code,
            Name = template.Name,
            Description = template.Description,
            Benefits = template.Benefits,
            IsRefundable = template.IsRefundable,
            IsChangeable = template.IsChangeable,
            ChangeFee = template.ChangeFee,
            HasSeatSelection = template.HasSeatSelection,
            HasBaggageIncluded = template.HasBaggageIncluded,
            HasPriorityBoarding = template.HasPriorityBoarding,
            HasLoungeAccess = template.HasLoungeAccess,
            PriceMultiplier = template.PriceMultiplier,
            BaseFare = baseFare,
            TotalFare = Math.Round(baseFare * template.PriceMultiplier, 2)
        };
    }
}