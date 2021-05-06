namespace Microsoft.Extensions.DependencyInjection
{
    using SSIMediator.AriesFrameworkCustom;
    using Hyperledger.Aries.Configuration;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Allows default agent configuration
        /// </summary>
        /// <param name="aApplicationBuilder">App.</param>
        public static void UseAriesFramework(this IApplicationBuilder aApplicationBuilder)
        {
            UseAriesFramework<CustomMediationAgent>(aApplicationBuilder);
        }

        /// <summary>
        /// Allows agent configuration by specifying a custom middleware
        /// </summary>
        /// <param name="aApplicationBuilder">App.</param>
        public static void UseAriesFramework<T>(this IApplicationBuilder aApplicationBuilder)
        {
            AgentOptions options =
              aApplicationBuilder.ApplicationServices.GetRequiredService<IOptions<AgentOptions>>().Value;

            aApplicationBuilder.UseMiddleware<T>();
        }
    }
}
