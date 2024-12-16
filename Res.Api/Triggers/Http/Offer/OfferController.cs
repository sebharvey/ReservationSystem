using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Res.Api.Core.Services;
using Res.Api.Models;

namespace Res.Api.Triggers.Http.Offer
{
    public class CheckInController
    {
        private readonly ILogger<CheckInController> _logger;
        private readonly IOfferService _offerService;

        private readonly JsonSerializerOptions _jsonOptions;

        public CheckInController(ILogger<CheckInController> logger, IOfferService offerService)
        {
            _logger = logger;
            _offerService = offerService;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        /// <summary>
        /// Runs an availability search
        /// {
        ///     "From": "LHR",
        ///     "To": "JFK",
        ///     "Date": "2025-05-01",
        ///     "Currency": "GBP",
        ///     "Passengers": [
        ///         {
        ///             "Ptc": "ADT",
        ///             "Total": 1
        ///         }
        ///     ]
        /// }   
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [Function("Search")]
        public async Task<IActionResult> Search([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "offer/search")] HttpRequest req)
        {
            // Read request body asynchronously
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Log the raw request for debugging
            _logger.LogDebug($"Raw auth request: {requestBody}");

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is empty" });
            }

            SearchRequest searchRequest;
            try
            {
                searchRequest = JsonSerializer.Deserialize<SearchRequest>(requestBody, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse authentication request");
                return new BadRequestObjectResult(new { error = "Invalid JSON format" });
            }

            return new OkObjectResult(await _offerService.Search(searchRequest));
        }

        [Function("Summary")]
        public async Task<IActionResult> Summary([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "offer/summary")] HttpRequest req)
        {
            // Read request body asynchronously
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Log the raw request for debugging
            _logger.LogDebug($"Raw auth request: {requestBody}");

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult(new { error = "Request body is empty" });
            }

            SummaryRequest summaryRequest;
            try
            {
                summaryRequest = JsonSerializer.Deserialize<SummaryRequest>(requestBody, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse authentication request");
                return new BadRequestObjectResult(new { error = "Invalid JSON format" });
            }

            return new OkObjectResult(await _offerService.Summary(summaryRequest));




            throw new NotImplementedException();
        }
    }
}