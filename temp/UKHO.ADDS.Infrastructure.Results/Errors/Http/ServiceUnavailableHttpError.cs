using System.Net;

namespace UKHO.ADDS.Infrastructure.Results.Errors.Http
{
    public sealed class ServiceUnavailableHttpError : HttpError
    {
        private const string DefaultMessage = "The service is unavailable";

        public ServiceUnavailableHttpError()
            : base(HttpStatusCode.ServiceUnavailable, DefaultMessage, new Dictionary<string, object>())
        {
        }

        public ServiceUnavailableHttpError(IDictionary<string, object> properties)
            : base(HttpStatusCode.ServiceUnavailable, DefaultMessage, properties)
        {
        }

        public ServiceUnavailableHttpError(string message, IDictionary<string, object> properties)
            : base(HttpStatusCode.ServiceUnavailable, SetMessage(message, DefaultMessage), properties)
        {
        }
    }
}


