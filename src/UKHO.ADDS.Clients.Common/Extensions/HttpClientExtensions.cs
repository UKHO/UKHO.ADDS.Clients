using System.Net.Http.Headers;
using UKHO.ADDS.Clients.Common.Authentication;

namespace UKHO.ADDS.Clients.Common.Extensions
{
    internal static class HttpClientExtensions
    {
        /// <summary>
        ///     Sets Authorization header
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="authTokenProvider"></param>
        /// <returns></returns>
        public static async Task SetAuthenticationHeaderAsync(this HttpClient httpClient, IAuthenticationTokenProvider authTokenProvider)
        {
            var token = await authTokenProvider.GetTokenAsync();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        }
    }
}
