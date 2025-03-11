using System.Net;

namespace UKHO.ADDS.Infrastructure.Results.Errors.Http
{
    public sealed class UnauthorizedHttpError : HttpError
    {
        private const string DefaultMessage = "The request was unauthorized";

        public UnauthorizedHttpError()
            : base(HttpStatusCode.Unauthorized, DefaultMessage, new Dictionary<string, object>())
        {
        }

        public UnauthorizedHttpError(IDictionary<string, object> properties)
            : base(HttpStatusCode.Unauthorized, DefaultMessage, properties)
        {
        }

        public UnauthorizedHttpError(string message, IDictionary<string, object> properties)
            : base(HttpStatusCode.Unauthorized, SetMessage(message, DefaultMessage), properties)
        {
        }
    }
}



