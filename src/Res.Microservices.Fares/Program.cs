using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Res.Microservices.Fares.Application.Interfaces;
using Res.Microservices.Fares.Application.Services;
using Res.Microservices.Fares.Configuration;
using Res.Microservices.Fares.Infrastructure.Services;

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

                    services.Configure<ServiceSettings>(context.Configuration.GetSection("ServiceSettings"));

                    services.AddScoped<IClaudeClient>(sp =>
                    {
                        var settings = sp.GetRequiredService<IOptions<ServiceSettings>>().Value;
                        var logger = sp.GetRequiredService<ILogger<MockClaudeClient>>();

                        if (settings.Mode == ServiceMode.Development)
                        {
                            return new MockClaudeClient(logger);
                        }

                        return new ClaudeClient(settings.AnthropicApiKey);
                    });

                    services.AddScoped<IPricingService, PricingService>();
                })
                .Build();
            
            host.Run();
        }
    }
}