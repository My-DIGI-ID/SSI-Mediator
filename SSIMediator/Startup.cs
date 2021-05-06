using SSIMediator.Agent;
using SSIMediator.Authentication;
using SSIMediator.Exceptions;
using Hyperledger.Aries.Features.BasicMessage;
using Hyperledger.Aries.Features.TrustPing;
using Hyperledger.Aries.Routing;
using Hyperledger.Aries.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace SSIMediator
{
    /// <inheritdoc /> 
    public class Startup
    {
        private static readonly string _appVersion = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        /// <inheritdoc /> 
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <inheritdoc /> 
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        /// <inheritdoc /> 
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options => { options.AllowEmptyInputInBodyModelBinding = true; })
                .AddNewtonsoftJson();


            services.AddAriesFramework(builder =>
            {
                builder.RegisterMediatorAgent<OwnMediatorAgent>(config =>
                {
                    config.WalletConfiguration = new WalletConfiguration { Id = Environment.GetEnvironmentVariable("GW_WALLET_ID") };
                    config.WalletCredentials = new WalletCredentials { Key = Environment.GetEnvironmentVariable("GW_WALLET_KEY") };

                    config.AgentImageUri = Environment.GetEnvironmentVariable("IMG_URL");
                    config.EndpointUri = Environment.GetEnvironmentVariable("ENDPOINT_URI");

                    config.AgentName = Environment.GetEnvironmentVariable("GW_NAME");
                });
            });

            services.AddMessageHandler<CustomMediatorForwardHandler>();
            services.AddMessageHandler<CustomRoutingInboxHandler>();
            services.AddMessageHandler<RoutingInboxHandler>();
            services.AddMessageHandler<DefaultBasicMessageHandler>();
            services.AddMessageHandler<DefaultTrustPingMessageHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// <inheritdoc /> 
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseMiddleware<ExceptionHandler>();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseMediatorDiscovery();

            app.UseAriesFramework();
        }
    }
}