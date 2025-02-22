using System.Net;

namespace UKHO.ADDS.Infrastructure.Results.Errors.Http
{
    public sealed class DownstreamServiceHttpError : AbstractHttpError
    {
        private const string DefaultMessage = "A downstream service error occured";

        public DownstreamServiceHttpError()
            : base(HttpStatusCode.BadGateway, DefaultMessage, new Dictionary<string, object>())
        {
        }

        public DownstreamServiceHttpError(IDictionary<string, object> properties)
            : base(HttpStatusCode.BadGateway, DefaultMessage, properties)
        {
        }

        public DownstreamServiceHttpError(string message, IDictionary<string, object> properties)
            : base(HttpStatusCode.BadGateway, SetMessage(message, DefaultMessage), properties)
        {
        }
    }
}
