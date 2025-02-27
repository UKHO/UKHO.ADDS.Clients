using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Factories
{
    [ExcludeFromCodeCoverage]
    internal class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
