using System.Net;

namespace UKHO.ADDS.Infrastructure.Results.Errors.Http
{
    public sealed class HttpError : AbstractHttpError
    {
        public HttpError(HttpStatusCode statusCode)
            : base(statusCode, $"An HTTP error occured, status code {statusCode}", new Dictionary<string, object>())
        {
        }

        public HttpError(HttpStatusCode statusCode, IDictionary<string, object> properties)
            : base(statusCode, $"An HTTP error occured, status code {statusCode}", properties)
        {
        }

        public HttpError(HttpStatusCode statusCode, string message, IDictionary<string, object> properties)
            : base(statusCode, message, properties)
        {
        }
    }
}

