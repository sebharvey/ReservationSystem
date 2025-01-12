using Res.Api.Offer.Domain.Interfaces;
using Res.Api.Offer.Domain.Models;

namespace Res.Api.Offer.Application.Services
{
    public class FlightSearchService : IFlightSearchService
    {
        private readonly IInventoryService _inventoryService;
        private readonly IFaringService _faringService;

        public FlightSearchService(IInventoryService inventoryService, IFaringService faringService)
        {
            _inventoryService = inventoryService;
            _faringService = faringService;
        }

        public async Task<SearchResponse> SearchFlightsAsync(SearchRequest request)
        {
            var availability = await _inventoryService.GetAvailabilityAsync(
                request.From,
                request.To,
                request.Date
            );

            var flightNumbers = availability.Select(a => a.FlightNumber).ToList();
            var prices = await _faringService.GetPricesAsync(flightNumbers, request.Currency);

            var flights = availability.Select(avail => new Flight
            {
                FlightNo = avail.FlightNumber,
                AircraftType = avail.AircraftType,
                DepartureDateTime = avail.DepartureTime,
                ArrivalDateTime = avail.ArrivalTime,
                Offers = CreateOffers(avail, prices.FirstOrDefault(p => p.FlightNumber == avail.FlightNumber))
            }).ToList();

            return new SearchResponse { Flights = flights };
        }

        private List<Domain.Models.Offer> CreateOffers(FlightAvailability avail, FlightPrice price)
        {
            if (price == null) return new List<Domain.Models.Offer>();

            return new List<Domain.Models.Offer>
            {
                new()
                {
                    OfferId = $"{avail.FlightNumber}-Y-{Guid.NewGuid()}",
                    BookingClass = "Y",
                    Price = price.EconomyPrice
                },
                new()
                {
                    OfferId = $"{avail.FlightNumber}-W-{Guid.NewGuid()}",
                    BookingClass = "W",
                    Price = price.PremiumPrice
                },
                new()
                {
                    OfferId = $"{avail.FlightNumber}-J-{Guid.NewGuid()}",
                    BookingClass = "J",
                    Price = price.BusinessPrice
                }
            };
        }
    }
}