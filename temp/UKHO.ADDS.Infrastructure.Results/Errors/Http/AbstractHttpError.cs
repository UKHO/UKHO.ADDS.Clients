using System.Net;

namespace UKHO.ADDS.Infrastructure.Results.Errors.Http
{
    public abstract class AbstractHttpError : Error
    {
        protected AbstractHttpError(HttpStatusCode statusCode)
            : base($"An HTTP error occured, status code {statusCode}", new Dictionary<string, object> { { "StatusCode", statusCode } })
        {
        }

        protected AbstractHttpError(HttpStatusCode statusCode, string message, IDictionary<string, object> properties)
            : base(message, MergeProperties(properties, new Dictionary<string, object> { { "StatusCode", statusCode } }))
        {
        }

        public HttpStatusCode StatusCode => (HttpStatusCode)Metadata["StatusCode"];

        private static IReadOnlyDictionary<string, object> MergeProperties(IDictionary<string, object> properties0, IDictionary<string, object> properties1)
        {
            return new Dictionary<string, object>(properties0.Concat(properties1.Where(kvp => !properties0.ContainsKey(kvp.Key))));
        }

        protected static string SetMessage(string message, string defaultMessage) => string.IsNullOrWhiteSpace(message) ? defaultMessage : message;
    }
}
