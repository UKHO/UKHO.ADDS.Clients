using System.Net;

namespace UKHO.ADDS.Infrastructure.Results.Errors.Http
{
    public sealed class InternalServerHttpError : HttpError
    {
        private const string DefaultMessage = "An internal server error occured";

        public InternalServerHttpError()
            : base(HttpStatusCode.InternalServerError, DefaultMessage, new Dictionary<string, object>())
        {
        }

        public InternalServerHttpError(IDictionary<string, object> properties)
            : base(HttpStatusCode.InternalServerError, DefaultMessage, properties)
        {
        }

        public InternalServerHttpError(string message, IDictionary<string, object> properties)
            : base(HttpStatusCode.InternalServerError, SetMessage(message, DefaultMessage), properties)
        {
        }
    }
}

