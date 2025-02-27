using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.Readonly.Tests.Helpers
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Deserialises a json string to the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static T DeserialiseJson<T>(this string jsonString) => JsonCodec.Decode<T>(jsonString);
    }
}
