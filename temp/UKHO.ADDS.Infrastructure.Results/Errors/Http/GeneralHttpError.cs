using System.Net;

namespace UKHO.ADDS.Infrastructure.Results.Errors.Http
{
    public sealed class GeneralHttpError : HttpError
    {
        public GeneralHttpError(HttpStatusCode statusCode)
            : base(statusCode, $"An HTTP error occured, status code {statusCode}", new Dictionary<string, object>())
        {
        }

        public GeneralHttpError(HttpStatusCode statusCode, IDictionary<string, object> properties)
            : base(statusCode, $"An HTTP error occured, status code {statusCode}", properties)
        {
        }

        public GeneralHttpError(HttpStatusCode statusCode, string message, IDictionary<string, object> properties)
            : base(statusCode, message, properties)
        {
        }
    }
}

