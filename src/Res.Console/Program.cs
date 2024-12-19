using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Res.Application;
using Res.Application.ReservationSystem;
using Res.Core.Interfaces;
using Res.Infrastructure.Interfaces;
using Res.Tests.Data;

namespace Res.Console
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            await RunApplication(host.Services);
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders(); // Remove all default providers including Console
                    logging.AddDebug(); // Add only Debug provider
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient();
                    services.AddApplicationServices(context.Configuration);
                });

        static async Task RunApplication(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            try
            {
                var reservationSystem = serviceProvider.GetRequiredService<IReservationSystem>();
                var inventoryService = serviceProvider.GetRequiredService<IInventoryService>();
                var seatMapRepository = serviceProvider.GetRequiredService<ISeatMapRepository>();
                var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
                var userService = serviceProvider.GetRequiredService<IUserService>();

                var testData = new TestData(inventoryService, reservationSystem, seatMapRepository, userRepository, userService);
                testData.Initialize();

                while (true)
                {
                    bool isLoggedIn = false;
                    string token = null;

                    while (!isLoggedIn)
                    {
                        token = await Login(userService);
                        isLoggedIn = !string.IsNullOrEmpty(token);

                        if (!isLoggedIn)
                        {
                            System.Console.WriteLine("\nPress ENTER to retry or type EXIT to quit: ");
                            string input = System.Console.ReadLine()?.ToUpper() ?? "";

                            System.Console.Clear();

                            if (input == "EXIT") return;
                        }
                    }

                    System.Console.Clear();
                    DisplayHeader();

                    while (isLoggedIn)
                    {
                        string input = System.Console.ReadLine()?.ToUpper() ?? string.Empty;

                        if (input == "EXIT") return;

                        if (input == "LO")
                        {
                            await reservationSystem.ProcessCommand("IG", token);

                            isLoggedIn = false;
                            token = null;

                            System.Console.Clear();
                            break;
                        }

                        try
                        {
                            System.Console.Clear();
                            DisplayHeader();
                            System.Console.WriteLine($"\nCOMMAND: {input}\n");

                            var response = await reservationSystem.ProcessCommand(input, token);

                            System.Console.WriteLine(response.Response.ToString().ToUpper());
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine($"ERROR: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error initializing application: {ex.Message}");
            }
        }

        static async Task<string> Login(IUserService userService)
        {
            System.Console.Write("USER ID: ");
            string userId = System.Console.ReadLine();

            System.Console.Write("PASSWORD: ");
            string password = GetMaskedPassword();
            System.Console.WriteLine();

            var authResponse = userService.Authenticate(userId.ToUpper(), password);

            if (authResponse.Success)
            {
                return authResponse.Token;
            }

            System.Console.WriteLine("\nINVALID CREDENTIALS\n");
            return string.Empty;
        }

        static string GetMaskedPassword()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = System.Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password.Append(key.KeyChar);
                    System.Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                    System.Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            return password.ToString();
        }

        private static void DisplayHeader()
        {
            System.Console.WriteLine("AIRLINE RESERVATION SYSTEM");
            System.Console.WriteLine("TYPE 'HELP' FOR COMMANDS OR 'EXIT' TO QUIT");
            System.Console.WriteLine("----------------------------------------");
        }
    }
}