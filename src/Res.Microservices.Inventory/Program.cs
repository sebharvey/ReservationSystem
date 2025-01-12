using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Res.Microservices.Inventory.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Res.Microservices.Inventory.Application.Services;
using Res.Microservices.Inventory.Infrastructure.Data;
using Res.Microservices.Inventory.Infrastructure.Repositories;

namespace Res.Microservices.Inventory
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
                    services.AddDbContext<InventoryDbContext>(options => options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));

                    services.AddMemoryCache();
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();
                    services.AddHttpClient();

                    services.AddScoped<IFlightRepository, FlightRepository>();
                    services.AddScoped<IInventoryService, InventoryService>();
                })
                .Build();

            host.Run();
        }
    }
}