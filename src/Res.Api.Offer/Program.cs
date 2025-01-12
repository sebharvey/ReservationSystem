using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Res.Api.Offer.Application.Services;
using Res.Api.Offer.Domain.Interfaces;
using Res.Api.Offer.Infrastructure.Services;

namespace Res.Microservices.Fares
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders(); // Remove all default providers including Console
                    logging.AddDebug(); // Add only Debug provider
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureAppConfiguration(configurationBuilder =>
                {
                    configurationBuilder.AddCommandLine(args)

                        .SetBasePath(System.Environment.CurrentDirectory)
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();
                })
                .ConfigureFunctionsWebApplication()
                .ConfigureServices((context, services) =>
                {
                    services.AddMemoryCache();
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();
                    services.AddHttpClient();

                    services.AddHttpClient<IInventoryService, InventoryService>();
                    services.AddHttpClient<IFaringService, FaringService>();
                    services.AddScoped<IFlightSearchService, FlightSearchService>();

                })
                .Build();

            host.Run();
        }
    }
}