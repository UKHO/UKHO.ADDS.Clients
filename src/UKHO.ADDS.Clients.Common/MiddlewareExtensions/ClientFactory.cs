﻿using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace UKHO.ADDS.Clients.Common.MiddlewareExtensions
{
    public class ClientFactory(
        IAuthenticationProvider defaultAuthProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<ClientFactory> logger)
    {
        private readonly IAuthenticationProvider _defaultAuthProvider = defaultAuthProvider;

        /// <summary>
        /// Creates an instance of the specified Kiota client type using the provided authentication provider and an HttpClient
        /// obtained from the IHttpClientFactory. The client type must have a constructor that accepts an IRequestAdapter.
        /// </summary>
        /// <typeparam name="TClient">The Kiota client type to instantiate.</typeparam>
        /// <returns>An instance of the specified Kiota client.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the client type does not have a constructor accepting an IRequestAdapter.
        /// </exception>
        public TClient GetClient<TClient>(IAuthenticationProvider? authenticationProvider = null) where TClient : class
        {
            // Find a constructor that takes IRequestAdapter
            var ctor = typeof(TClient).GetConstructor([typeof(IRequestAdapter)]);
            if (ctor == null)
            {
                throw new InvalidOperationException($"{typeof(TClient).Name} must have a constructor with IRequestAdapter parameter.");
            }

            // If an authenitcationProvider is specified then use it, otherwise use the registered authentication provider in the Service Provider
            var provider = authenticationProvider ?? _defaultAuthProvider;
            if (provider == null)
            {
                throw new InvalidOperationException($"{typeof(TClient).Name} must have an Authentication Provider registered.");
            }

            // Create the Http client here to make sure it is configured correctly
            var httpClient = httpClientFactory.CreateClient(typeof(TClient).Name);
            logger.LogInformation("Creating client for {ClientType} with base address: {baseAddress}", typeof(TClient).Name, httpClient.BaseAddress);
            return (TClient)ctor.Invoke([new HttpClientRequestAdapter(provider, httpClient: httpClient)]);
        }
    }
}
