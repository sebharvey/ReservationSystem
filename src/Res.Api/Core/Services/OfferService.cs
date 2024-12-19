using Res.Api.Common.Helpers;
using Res.Api.Models;
using Res.Core.Interfaces;
using Res.Domain.Entities.Inventory;
using Res.Domain.Entities.Pnr;
using Res.Domain.Requests;

namespace Res.Api.Core.Services
{
    public class OfferService : IOfferService
    {
        private readonly IInventoryService _inventoryService;
        private readonly IFareService _fareService;

        public OfferService(IInventoryService inventoryService, IFareService fareService)
        {
            _inventoryService = inventoryService;
            _fareService = fareService;
        }

        public async Task<SearchResponse> Search(SearchRequest searchRequest)
        {

            List<FlightInventory> flights = await _inventoryService.SearchAvailability(new AvailabilityRequest
            {
                DepartureDate = searchRequest.Date.ToAirlineFormat(),
                Origin = searchRequest.From,
                Destination = searchRequest.To,
            });

            var flightResults = new List<FlightResult>();

            var bookingClasses = new[] { "J", "W", "Y" };

            foreach (var flight in flights)
            {
                var offers = new List<Offer>();

                foreach (var bookingClass in bookingClasses)
                {
                    var (baseFare, fareBases) = _fareService.CalculateSegmentPrices(new List<Segment>
                    {
                        new Segment
                        {
                            BookingClass = bookingClass
                        }
                    }, searchRequest.Currency);

                    offers.Add(new Offer
                    {
                        OfferId = OfferIdHelper.Encode(string.Join("-", flight.FlightNo, flight.DepartureDate, bookingClass)),
                        BookingClass = bookingClass,
                        Price = baseFare,
                        IsAvailable = true // todo IsAvailable needs to be set by the actual cabin availability and the total pax count
                    });
                }

                flightResults.Add(new FlightResult
                {
                    AircraftType = flight.AircraftType,
                    DepartureDateTime = $"{flight.DepartureDate}/{flight.DepartureTime}".FromAirlineDateAndTime(),
                    ArrivalDateTime = $"{flight.ArrivalDate}/{flight.ArrivalTime}".FromAirlineDateAndTime(),
                    FlightNo = flight.FlightNo,
                    Offers = offers
                });
            }

            return new SearchResponse
            {
                From = searchRequest.From,
                To = searchRequest.To,
                Date = searchRequest.Date.Date,
                Flights = flightResults.OrderBy(item => item.DepartureDateTime).ToList()
            };
        }

        public async Task<SummaryResponse> Summary(SummaryRequest summaryRequest)
        {
            List<SummaryResponse.SummarySegment> summarySegments = new List<SummaryResponse.SummarySegment>();

            foreach (var offerId in summaryRequest.OfferIds)
            {
                string[] parts = OfferIdHelper.Decode(offerId).Split("-");

                SummaryResponse.SummarySegment summarySegment = new SummaryResponse.SummarySegment
                {
                    OfferId = offerId,
                    FlightNumber = parts[0],
                    DepartureDate = parts[1].FromAirlineFormat(),
                    BookingClass = parts[2]
                };

                summarySegments.Add(summarySegment);
            }

            SummaryResponse summaryResponse = new SummaryResponse
            {
                Segments = summarySegments
            };

            return summaryResponse;
        }
    }
}