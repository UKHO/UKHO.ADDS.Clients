using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace UKHO.ADDS.Clients.Common.MiddlewareExtensions
{
    /// <summary>
    /// Service collection extensions for Kiota handlers.
    /// </summary>
    public static class KiotaServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Kiota handlers to the service collection.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to add the services to</param>
        /// <param name="kiotaClientFactory">Factory to get handler types</param>
        /// <returns><see cref="IServiceCollection"/> as per convention</returns>
        public static IServiceCollection AddKiotaHandlers(this IServiceCollection services)
        {
            // Dynamically load the Kiota handlers from the Client Factory
            var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
            // And register them in the DI container
            foreach (var handler in kiotaHandlers)
            {
                services.AddTransient(handler);
            }

            return services;
        }

        /// <summary>
        /// Adds the Kiota handlers to the http client builder.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="kiotaClientFactory">Factory to get handler types</param>
        /// <returns></returns>
        public static IHttpClientBuilder AttachKiotaHandlers(this IHttpClientBuilder builder)
        {
            // Dynamically load the Kiota handlers from the Client Factory
            var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
            // And attach them to the http client builder
            foreach (var handler in kiotaHandlers)
            {
                builder.AddHttpMessageHandler((sp) => (DelegatingHandler)sp.GetRequiredService(handler));
            }

            return builder;
        }

        /// <summary>
        /// Add a configured HTTP client for a specific client type using a configuration key.
        /// </summary>
        /// <typeparam name="TClient">FGenerated Kioat Client</typeparam>
        /// <param name="services">Service Collection for the target application</param>
        /// <param name="endpointConfigKey">EndPoint configuration value</param>
        /// <returns></returns>
        public static IHttpClientBuilder AddConfiguredHttpClient<TClient>(this IServiceCollection services, string endpointConfigKey)
                                                                            where TClient : class
        {
            return services.AddHttpClient<TClient>((provider, client) =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var endpoint = config[endpointConfigKey]!;
                client.BaseAddress = new Uri(endpoint);
            }).AttachKiotaHandlers();
        }
    }
}
