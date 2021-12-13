using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics;

namespace TeamsChannelTester
{
    class Program
    {
        public static void Main(string[] args)
        {
            // Switch to W3C Trace Context format to propagate distributed trace identifiers
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;

            InitTemporaryLogger();

            try
            {
                IHostBuilder builder = CreateWebHostBuilder(args);
                using (IHost host = builder.Build())
                {
                    host.Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .UseStartup<Startup>();
                });
        }

        private static void InitTemporaryLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateBootstrapLogger(); 
        }
    }
}
