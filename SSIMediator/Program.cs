using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;
using System;
using System.Linq;
using System.Net;
using System.Security.Authentication;

namespace SSIMediator
{
    /// <inheritdoc />
    public class Program
    {
        /// <inheritdoc />
        public static LoggingLevelSwitch LoggingLevelSwitch { get; set; }

        /// <inheritdoc />
        public static void Main(string[] args)
        {
            CheckEnvVar("DEPLOYMENT", "local", true);
            CheckEnvVar("LOGLEVEL", "2", true);
            CheckEnvVar("SERVICE", "ssi-mediation-agent", true);

            InitLogger();

            Log.Information("Application is starting...");

            CheckForEnviromentVariables();
            try
            {
                Log.Information("Starting web host");
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

        private static void InitLogger()
        {
            LoggingLevelSwitch = new LoggingLevelSwitch() { MinimumLevel = (LogEventLevel)int.Parse(Environment.GetEnvironmentVariable("LOGLEVEL")) };

            string serviceName = Environment.GetEnvironmentVariable("SERVICE");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LoggingLevelSwitch)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service-name", serviceName)
                .WriteTo.File(new ElasticsearchJsonFormatter(), $"log/{serviceName}-log.json", rollingInterval: RollingInterval.Day,
                            shared: true, flushToDiskInterval: TimeSpan.FromSeconds(1), retainedFileCountLimit: 10)
                .WriteTo.Console()
                .CreateLogger();
        }

        /// <inheritdoc />
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            System.Collections.Generic.List<string> enviroments = Environment.GetEnvironmentVariable("DEPLOYMENT").Split(",").ToList();

            if (enviroments.Contains("local"))
            {
                return Host.CreateDefaultBuilder(args)
                    .UseSerilog()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    });
            }
            else
            {
                return Host.CreateDefaultBuilder(args)
                    .UseSerilog()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseKestrel(options =>
                        {
                            options.ConfigureHttpsDefaults(options =>
                            {
                                options.SslProtocols = SslProtocols.Tls12;
                            });
                            options.Listen(IPAddress.Parse("0.0.0.0"), 80);
                            options.Listen(IPAddress.Parse("0.0.0.0"), 443, listenOptions =>
                            {
                                listenOptions.UseHttps("certs/ssi-certs.p12", Environment.GetEnvironmentVariable("CERT_KEY"));
                            });
                        });
                        webBuilder.UseSetting("https_port", "443");
                        webBuilder.UseStartup<Startup>();
                    });
            }
        }

        private static void CheckForEnviromentVariables()
        {
            CheckEnvVar("ENDPOINT_URI", "ENDPOINT", true);
            CheckEnvVar("IMG_URL", "LOGOURL", true);

            CheckEnvVar("DEPLOYMENT", "local", true);
            CheckEnvVar("LOGLEVEL", "2", true);
            CheckEnvVar("SERVICE", "ssi-mediation-agent", true);

            CheckEnvVar("GW_WALLET_ID", "SSI-MEDIATION-AGENT", true);
            CheckEnvVar("GW_WALLET_KEY", "myHighSecureKey", false);
            CheckEnvVar("GW_NAME", "SSI-MEDIATION-AGENT", true);

            CheckEnvVar("MOBILE_SECRETS", "mobiletoken", false);

            CheckEnvVar("CERT_KEY", "ssi-certs", false);

            CheckEnvVar("X-AUTH-TOKEN", "token", false);
        }

        private static void CheckEnvVar(string envVar, string defaultValue, bool enableLogging)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
            {
                Log.Information($"Environment Variable {envVar} was empty");
                Environment.SetEnvironmentVariable(envVar, defaultValue);
            }
            if (enableLogging)
            {
                Log.Information($"Environment Variable {envVar} = " + Environment.GetEnvironmentVariable(envVar));
            }
            else
            {
                Log.Information($"Environment Variable {envVar} = HIDDEN_VALUE");
            }
        }
    }
}
