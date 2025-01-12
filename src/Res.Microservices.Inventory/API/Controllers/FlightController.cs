using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Res.Microservices.Inventory.Application.DTOs;
using Res.Microservices.Inventory.Application.Interfaces;

namespace Res.Microservices.Inventory.API.Controllers
{
    public class FlightController
    {
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<FlightController> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public FlightController(IInventoryService inventoryService, ILogger<FlightController> logger)
        {
            _inventoryService = inventoryService;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        [Function("SearchFlights")]
        [OpenApiOperation(operationId: "SearchFlights", tags: new[] { "Flights" })]
        [OpenApiRequestBody("application/json", typeof(FlightSearchRequest))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<FlightSearchResponse>))]
        public async Task<HttpResponseData> SearchFlights([HttpTrigger(AuthorizationLevel.Function, "post", Route = "inventory/flights/search")] HttpRequestData req)
        {
            _logger.LogInformation("Processing flight search request");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var searchRequest = JsonSerializer.Deserialize<FlightSearchRequest>(requestBody, _jsonOptions);

                // Validate request
                if (searchRequest.DepartureDate == default)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid departure date");
                }

                if (string.IsNullOrEmpty(searchRequest.Origin) || string.IsNullOrEmpty(searchRequest.Destination))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Origin and destination are required");
                }

                var results = await _inventoryService.SearchFlights(searchRequest);
                return await CreateSuccessResponse(req, results);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid request format");
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing flight search request");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("ImportSchedule")]
        [OpenApiOperation(operationId: "ImportSchedule", tags: new[] { "Flights" })]
        [OpenApiRequestBody("application/json", typeof(ImportScheduleRequest))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(void))]
        public async Task<HttpResponseData> ImportSchedule([HttpTrigger(AuthorizationLevel.Function, "post", Route = "inventory/flights/import")] HttpRequestData req)
        {
            _logger.LogInformation("Processing schedule import request");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var importRequest = JsonSerializer.Deserialize<ImportScheduleRequest>(requestBody, _jsonOptions);

                //// Validate request
                //if (importRequest.StartDate == default || importRequest.EndDate == default)
                //{
                //    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid date range");
                //}

                //if (importRequest.StartDate > importRequest.EndDate)
                //{
                //    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Start date must be before or equal to end date");
                //}

                await _inventoryService.ImportSchedule(importRequest);
                return await CreateSuccessResponse(req);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid request format");
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid import request parameters");
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing schedule import");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }


        private async Task<HttpResponseData> CreateSuccessResponse<T>(HttpRequestData req, T data)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            var jsonString = JsonSerializer.Serialize(data, _jsonOptions);
            await response.WriteStringAsync(jsonString);
            return response;
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");
            var jsonString = JsonSerializer.Serialize(new { error = message }, _jsonOptions);
            await response.WriteStringAsync(jsonString);
            return response;
        }
        private async Task<HttpResponseData> CreateSuccessResponse(HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}