using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.FileShareClient.Internal
{
    internal static class HttpResponseMessageExtensions
    {
        /// <summary>
        ///     Reads response body json as given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpResponseMessage"></param>
        /// <returns></returns>
        public static async Task<T> ReadAsTypeAsync<T>(this HttpResponseMessage httpResponseMessage)
        {
            var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();

            var type = typeof(T);

            return JsonCodec.Decode<T>(bodyJson);
        }

        public static async Task<Stream> ReadAsStreamAsync(this HttpResponseMessage httpResponseMessage) => await httpResponseMessage.Content.ReadAsStreamAsync();
    }
}
