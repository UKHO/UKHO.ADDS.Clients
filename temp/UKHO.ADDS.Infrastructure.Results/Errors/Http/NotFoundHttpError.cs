using System.Net;

namespace UKHO.ADDS.Infrastructure.Results.Errors.Http
{
    public sealed class NotFoundHttpError : AbstractHttpError
    {
        private const string DefaultMessage = "The requested resource was not found";

        public NotFoundHttpError()
            : base(HttpStatusCode.NotFound, DefaultMessage, new Dictionary<string, object>())
        {
        }

        public NotFoundHttpError(IDictionary<string, object> properties)
            : base(HttpStatusCode.NotFound, DefaultMessage, properties)
        {
        }

        public NotFoundHttpError(string message, IDictionary<string, object> properties)
            : base(HttpStatusCode.NotFound, SetMessage(message, DefaultMessage), properties)
        {
        }
    }
}

