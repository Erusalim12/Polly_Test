using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Polly_Test
{
    public class Program
    {
        public static void Main(string[] args)
        {

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.WithProperty("app", "ip_changer")
                .WriteTo.Console(LogEventLevel.Warning)
                .CreateLogger();
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                  Host.CreateDefaultBuilder(args)
                      .UseSerilog()
                      .ConfigureWebHostDefaults(webBuilder =>
                      {
                          webBuilder.UseKestrel(t =>
                          {
#if DEBUG
                              t.ListenAnyIP(6323);
#else
                        t.ListenAnyIP(5555);
#endif
                          });
                          webBuilder.UseStartup<Startup>();
                      });
    }


}
 
