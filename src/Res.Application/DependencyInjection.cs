using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Res.Application.Commands;
using Res.Application.Interfaces;
using Res.Application.Parsers.Factory;
using Res.Application.ReservationSystem;
using Res.Core.Interfaces;
using Res.Core.Services;
using Res.Infrastructure.Data;
using Res.Infrastructure.Interfaces;
using Res.Infrastructure.Repositories;

namespace Res.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IPnrDataManager>(provider => new PnrDataManager(configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<IFlightInventoryDataManager>(provider => new FlightInventoryDataManager(configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<IFlightSeatInventoryDataManager>(provider => new FlightSeatInventoryDataManager(configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<ISeatConfigurationDataManager>(provider => new SeatConfigurationDataManager(configuration.GetConnectionString("DefaultConnection")));

            // Register repositories as singletons since they're holding in-memory data
            services.AddSingleton<IPnrRepository, PnrRepository>();
            services.AddSingleton<IInventoryRepository, InventoryRepository>();
            services.AddSingleton<ISeatMapRepository, SeatMapRepository>();
            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<IFareRepository, FareRepository>();

            services.AddSingleton<ICommandParserFactory, CommandParserFactory>();

            // Register HttpClient for APIS service
            services.AddHttpClient<IApisService, ApisService>(client =>
            {
                client.BaseAddress = new Uri("https://apis.example.com/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                // Add any other default headers or timeouts
            });
            services.AddHttpClient<IPaymentService, PaymentService>(client =>
            {
                client.BaseAddress = new Uri("https://payment.gateway/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                // Add any other default headers
            });
            
            // Register core services
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<ITicketingService, TicketingService>();
            services.AddScoped<IFareService, FareService>();
            services.AddScoped<ISpecialServiceRequestsService, SpecialServiceRequestsService>();
            services.AddScoped<ICheckInService, CheckInService>();
            services.AddScoped<IApisService, ApisService>();
            services.AddScoped<IBoardingPassService, BoardingPassService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ISeatService, SeatService>();

            // Register application services
            services.AddScoped<IReservationCommands, ReservationCommands>();

            // Register as a singleton to maintain persistence between requests
            services.AddSingleton<IReservationSystem, ReservationSystem.ReservationSystem>();

            return services;
        }
    }
}