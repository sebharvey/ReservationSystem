using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Res.Api.Offer.Domain.Interfaces;
using Res.Api.Offer.Domain.Models;

namespace Res.Api.Offer.Triggers.Http
{
    public class SearchFlightsFunction
    {
        private readonly IFlightSearchService _flightSearchService;
        private readonly ILogger<SearchFlightsFunction> _logger;

        public SearchFlightsFunction(IFlightSearchService flightSearchService, ILogger<SearchFlightsFunction> logger)
        {
            _flightSearchService = flightSearchService;
            _logger = logger;
        }

        [Function("SearchFlights")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "offer/search")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("Processing flight search request");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var searchRequest = JsonSerializer.Deserialize<SearchRequest>(requestBody);

                var result = await _flightSearchService.SearchFlightsAsync(searchRequest);

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing flight search request");
                return new StatusCodeResult(500);
            }
        }
    }
}