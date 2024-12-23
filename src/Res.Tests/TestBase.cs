using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Res.Application;
using Res.Application.ReservationSystem;
using Res.Core.Interfaces;
using Res.Domain.Dto;
using Res.Domain.Entities.CheckIn;
using Res.Infrastructure.Interfaces;
using Res.Tests.Data;
using Res.Tests.Mocks;

namespace Res.Tests
{
    public class TestBase : IDisposable
    {
        protected IServiceProvider ServiceProvider;
        protected IServiceScope Scope;
        protected IReservationSystem ReservationSystem;
        protected IReservationService ReservationService;
        protected IInventoryRepository InventoryRepository;
        //protected IPnrRepository PnrRepository;
        protected ISeatMapRepository SeatMapRepository;
        protected IUserRepository UserRepository;
        protected IUserService UserService;
        protected string Token;

        protected TestBase(bool useMocks = false)
        {
            var services = new ServiceCollection();
            services.AddLogging(configure => configure.AddConsole());

            var configValues = new Dictionary<string, string>
            {
                {"Payment:MerchantId", "TEST_MERCHANT"},
                {"Payment:ApiKey", "TEST_API_KEY"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            if (useMocks)
            {
                ConfigureMockServices(services, configuration);
            }
            else
            {
                ConfigureTestServices(services, configuration);
            }

            ServiceProvider = services.BuildServiceProvider();
            Scope = ServiceProvider.CreateScope();

            InitializeServices();
            InitializeTestData();
            InitializeAuth();
        }

        private void ConfigureMockServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(_ => Mock.Of<IInventoryRepository>());
            //services.AddSingleton(_ => Mock.Of<IPnrRepository>());

            services.AddScoped(_ => Mock.Of<IApisService>(apis =>
                apis.ValidateApisData(It.IsAny<ApisData>()) == Task.FromResult(true) &&
                apis.SubmitApisData(It.IsAny<ApisData>()) == Task.FromResult(true)));

            services.AddScoped(_ => Mock.Of<IPaymentService>(payment =>
                payment.Authorize(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>())
                == Task.FromResult(new PaymentAuthorizationResult
                {
                    Success = true,
                    AuthorizationCode = "TEST_AUTH",
                    TransactionId = "TEST_TRANS"
                }) &&
                payment.Capture(
                    It.IsAny<string>(), It.IsAny<decimal>(),
                    It.IsAny<string>(), It.IsAny<string>())
                == Task.FromResult(new PaymentCaptureResult
                {
                    Success = true,
                    CaptureCode = "TEST_CAPTURE"
                })));

            services.AddApplicationServices(configuration);
        }

        private void ConfigureTestServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IApisService, TestApisService>();
            services.AddScoped<IPaymentService, TestPaymentService>();
            services.AddHttpClient();
            services.AddApplicationServices(configuration);
        }

        private void InitializeServices()
        {
            ReservationSystem = Scope.ServiceProvider.GetRequiredService<IReservationSystem>();
            ReservationService = Scope.ServiceProvider.GetRequiredService<IReservationService>();
            InventoryRepository = Scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            //PnrRepository = Scope.ServiceProvider.GetRequiredService<IPnrRepository>();
            SeatMapRepository = Scope.ServiceProvider.GetRequiredService<ISeatMapRepository>();
            UserRepository = Scope.ServiceProvider.GetRequiredService<IUserRepository>();
            UserService = Scope.ServiceProvider.GetRequiredService<IUserService>();
        }

        private void InitializeTestData()
        {
            var inventoryService = Scope.ServiceProvider.GetRequiredService<IInventoryService>();
            var testData = new TestData(
                inventoryService,
                ReservationSystem,
                SeatMapRepository,
                UserRepository,
                UserService
            );
            testData.Initialize(false);
        }

        private void InitializeAuth()
        {
            var loginResponse = UserService.Authenticate("SH", "123");
            Token = loginResponse.Token;
        }

        public void Dispose()
        {
            Scope.Dispose();
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}