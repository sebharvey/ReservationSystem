using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Res.Application;
using Res.Application.ReservationSystem;
using Res.Core.Interfaces;
using Res.Infrastructure.Interfaces;
using Res.Tests.Data;
using Res.Api.Core.Services;
using ISeatService = Res.Api.Core.Services.ISeatService;

namespace Res.Api
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
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();
                    services.AddHttpClient();
                    services.AddApplicationServices(context.Configuration);

                    services.AddScoped<IOfferService, OfferService>();
                    services.AddScoped<IOrderService, OrderService>();
                    services.AddScoped<ISeatService, SeatService>();
                    services.AddScoped<ICheckinService, CheckinService>();
                    services.AddScoped<IStatusService, StatusService>();
                })
                .Build();

            await RunApplication(host.Services);

            host.Run();
        }
        static async Task RunApplication(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var reservationSystem = serviceProvider.GetRequiredService<IReservationSystem>();
            var inventoryService = serviceProvider.GetRequiredService<IInventoryService>();
            var seatMapRepository = serviceProvider.GetRequiredService<ISeatMapRepository>();
            var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
            var userService = serviceProvider.GetRequiredService<IUserService>();

            var testData = new TestData(inventoryService, reservationSystem, seatMapRepository, userRepository, userService);
            testData.Initialize();
        }
    }
}