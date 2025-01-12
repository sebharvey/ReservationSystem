using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Res.Microservices.Inventory.Application.Interfaces;
using Res.Microservices.Inventory.Application.Services;
using Res.Microservices.Inventory.Infrastructure.Data;
using Res.Microservices.Inventory.Infrastructure.Repositories;

public class Program
{
    static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<InventoryDbContext>(options =>
                    options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));

                services.AddMemoryCache();
                services.AddApplicationInsightsTelemetryWorkerService();
                services.ConfigureFunctionsApplicationInsights();
                services.AddHttpClient();

                services.AddScoped<IFlightRepository, FlightRepository>();
                services.AddScoped<IInventoryService, InventoryService>();

                // Add OpenAPI configuration
                services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
                    new OpenApiConfigurationOptions
                    {
                        Info = new OpenApiInfo
                        {
                            Version = "1.0",
                            Title = "Inventory API",
                            Description = "API for managing flight inventory and seat allocations"
                        },
                        Servers = new List<OpenApiServer>
                        {
                            new OpenApiServer { Url = "http://localhost:7044" }
                        }
                    });
            })
            .ConfigureOpenApi()   
            .Build();

        await host.RunAsync();
    }
}