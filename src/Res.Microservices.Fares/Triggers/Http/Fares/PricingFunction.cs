using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Res.Microservices.Fares.Application.DTOs;
using Res.Microservices.Fares.Application.Interfaces;

namespace Res.Microservices.Fares.Triggers.Http.Fares
{
    public class PricingFunction
    {
        private readonly IPricingService _pricingService;
        private readonly ILogger<PricingFunction> _logger;

        public PricingFunction(IPricingService pricingService, ILogger<PricingFunction> logger)
        {
            _pricingService = pricingService;
            _logger = logger;
        }

        [Function("GetFares")]
        public async Task<HttpResponseData> GetFares([HttpTrigger(AuthorizationLevel.Function, "post", Route = "pricing/fares")] HttpRequestData req)
        {
            _logger.LogInformation("Processing pricing request");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var pricingRequest = JsonSerializer.Deserialize<PricingRequestDto>(requestBody);

                var result = await _pricingService.GetPricingRecommendation(pricingRequest);

                await response.WriteStringAsync(JsonSerializer.Serialize(result));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pricing request");

                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    error = ex.Message
                }));
                return response;
            }
        }
    }
}