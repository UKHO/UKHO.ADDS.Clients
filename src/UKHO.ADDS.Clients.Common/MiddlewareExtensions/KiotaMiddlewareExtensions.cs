using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace UKHO.ADDS.Clients.Common.MiddlewareExtensions
{
    /// <summary>
    /// Extension methods for registering Kiota handlers, client factory, and authentication provider in the service collection.
    /// </summary>
    public static class KiotaServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the default Kiota handlers, client factory, and authentication provider in the service collection.
        /// </summary>
        /// <typeparam name="T">The type of authentication provider to use with Kiota clients.</typeparam>
        /// <param name="services">The service collection to register Kiota services with.</param>
        /// <param name="authProvider">The authentication provider to use for client creation.</param>
        public static void AddKiotaDefaults<T>(this IServiceCollection services, T authProvider) where T : IAuthenticationProvider
        {
            services.AddKiotaHandlers();
            services.AddSingleton<ClientFactory>();
            services.AddSingleton<IAuthenticationProvider>(authProvider);
        }

        /// <summary>
        /// Registers all Kiota middleware handlers in the service collection.
        /// </summary>
        /// <param name="services">The service collection to add the handlers to.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddKiotaHandlers(this IServiceCollection services)
        {
            var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
            foreach (var handler in kiotaHandlers)
            {
                services.AddTransient(handler);
            }
            return services;
        }

        /// <summary>
        /// Attaches all registered Kiota middleware handlers to the HTTP client builder.
        /// </summary>
        /// <param name="builder">The HTTP client builder to attach handlers to.</param>
        /// <returns>The updated HTTP client builder.</returns>
        private static IHttpClientBuilder AttachKiotaHandlers(this IHttpClientBuilder builder)
        {
            var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
            foreach (var handler in kiotaHandlers)
            {
                builder.AddHttpMessageHandler(sp => (DelegatingHandler)sp.GetRequiredService(handler));
            }
            return builder;
        }

        /// <summary>
        /// Registers and configures an HTTP client for a specific Kiota client type using a configuration key for the endpoint.
        /// </summary>
        /// <typeparam name="TClient">The Kiota client type to register.</typeparam>
        /// <param name="services">The service collection to register the HTTP client with.</param>
        /// <param name="endpointConfigKey">The configuration key for the endpoint URL.</param>
        /// <param name="headers">Optional default headers to add to the HTTP client.</param>
        /// <returns>The HTTP client builder for further configuration.</returns>
        private static IHttpClientBuilder AddConfiguredHttpClient<TClient>(
            this IServiceCollection services,
            string endpointConfigKey,
            IDictionary<string, string>? headers = null)
            where TClient : class
        {
            return services.AddHttpClient<TClient>((provider, client) =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var endpoint = config[endpointConfigKey]!;
                client.BaseAddress = new Uri(endpoint);

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
            }).AttachKiotaHandlers();
        }

        /// <summary>
        /// Registers a Kiota client in the service collection, including its configured HTTP client and factory.
        /// </summary>
        /// <typeparam name="TClient">The Kiota client type to register.</typeparam>
        /// <param name="services">The service collection to register the client with.</param>
        /// <param name="endpointConfigKey">The configuration key for the endpoint URL.</param>
        /// <param name="headers">Optional default headers to add to the HTTP client.</param>
        public static void RegisterKiotaClient<TClient>(
            this IServiceCollection services,
            string endpointConfigKey,
            IDictionary<string, string>? headers = null)
            where TClient : class
        {
            services.AddConfiguredHttpClient<TClient>(endpointConfigKey, headers);
            services.AddSingleton(sp => sp.GetRequiredService<ClientFactory>().GetClient<TClient>());
        }
    }
}
