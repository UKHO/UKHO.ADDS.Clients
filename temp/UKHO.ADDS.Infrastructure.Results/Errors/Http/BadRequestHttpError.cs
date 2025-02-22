using System.Net;

namespace UKHO.ADDS.Infrastructure.Results.Errors.Http
{
    public sealed class BadRequestHttpError : AbstractHttpError
    {
        private const string DefaultMessage = "Bad request";

        public BadRequestHttpError()
            : base(HttpStatusCode.BadRequest, DefaultMessage, new Dictionary<string, object>())
        {
        }

        public BadRequestHttpError(IDictionary<string, object> properties)
            : base(HttpStatusCode.BadRequest, DefaultMessage, properties)
        {
        }

        public BadRequestHttpError(string message, IDictionary<string, object> properties)
            : base(HttpStatusCode.BadRequest, SetMessage(message, DefaultMessage), properties)
        {
        }
    }
}
