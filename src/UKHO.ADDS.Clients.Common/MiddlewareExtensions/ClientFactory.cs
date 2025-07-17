using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace UKHO.ADDS.Clients.Common.MiddlewareExtensions
{
    public class ClientFactory(IAuthenticationProvider authProvider, IHttpClientFactory httpClientFactory)
    {
        public TClient GetClient<TClient>() where TClient : class
        {
            // Find a constructor that takes IRequestAdapter
            var ctor = typeof(TClient).GetConstructor([typeof(IRequestAdapter)]);
            if (ctor == null)
            {
                throw new InvalidOperationException($"{typeof(TClient).Name} must have a constructor with IRequestAdapter parameter.");
            }

            // Create the Http client here to make sure it is configured correctly
            var httpClient = httpClientFactory.CreateClient(typeof(TClient).Name);

            return (TClient)ctor.Invoke([new HttpClientRequestAdapter(authProvider, httpClient: httpClient)]);
        }
    }
}
