using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Res.Microservices.Inventory.Application.DTOs;
using Res.Microservices.Inventory.Application.Interfaces;

namespace Res.Microservices.Inventory.API.Controllers
{
    public class SeatsController
    {
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<SeatsController> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public SeatsController(IInventoryService inventoryService, ILogger<SeatsController> logger)
        {
            _inventoryService = inventoryService;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        [Function("Allocations")]
        public async Task<HttpResponseData> Allocations(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "inventory/seats/allocations")] HttpRequestData req)
        {
            _logger.LogInformation("Processing allocation request");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var seatMapRequest = JsonSerializer.Deserialize<AllocationRequest>(requestBody, _jsonOptions);

                // Validate request
                if (seatMapRequest.DepartureDate == default)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid departure date");
                }

                if (string.IsNullOrEmpty(seatMapRequest.FlightNo))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Flight number is required");
                }

                var seatMap = await _inventoryService.GetSeatMap(seatMapRequest);
                return await CreateSuccessResponse(req, seatMap);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid request format");
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing seat map request");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("AllocateSeat")]
        public async Task<HttpResponseData> AllocateSeat(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "inventory/seats/allocate")] HttpRequestData req)
        {
            _logger.LogInformation("Processing seat allocation request");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var allocationRequest = JsonSerializer.Deserialize<SeatAllocationRequest>(requestBody, _jsonOptions);

                // Validate request
                if (allocationRequest.DepartureDate == default)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid departure date");
                }

                if (string.IsNullOrEmpty(allocationRequest.FlightNo) ||
                    string.IsNullOrEmpty(allocationRequest.SeatNumber) ||
                    string.IsNullOrEmpty(allocationRequest.RecordLocator))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                        "Flight number, seat number, and record locator are required");
                }

                var success = await _inventoryService.AllocateSeat(allocationRequest);

                if (!success)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                        "Unable to allocate seat - may already be allocated");
                }

                return await CreateSuccessResponse(req);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid request format");
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing seat allocation request");
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