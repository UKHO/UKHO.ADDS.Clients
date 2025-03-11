using System.Net;
using UKHO.ADDS.Infrastructure.Results.Errors;
using UKHO.ADDS.Infrastructure.Results.Errors.Http;

namespace UKHO.ADDS.Infrastructure.Results
{
    public sealed class ErrorFactory
    {
        public static IError CreateError(HttpStatusCode statusCode) => CreateError(statusCode, new Dictionary<string, object>());

        public static IError CreateError(HttpStatusCode statusCode, IDictionary<string, object> properties) => CreateError(statusCode, string.Empty, properties);

        public static IError CreateError(HttpStatusCode statusCode, string message, IDictionary<string, object> properties) =>
            statusCode switch
            {
                HttpStatusCode.BadRequest => new BadRequestHttpError(message, properties),
                HttpStatusCode.Unauthorized => new UnauthorizedHttpError(message, properties),
                HttpStatusCode.ServiceUnavailable => new ServiceUnavailableHttpError(message, properties),
                HttpStatusCode.BadGateway => new DownstreamServiceHttpError(message, properties),
                HttpStatusCode.NotFound => new NotFoundHttpError(message, properties),
                HttpStatusCode.InternalServerError => new InternalServerHttpError(message, properties),
                _ => new GeneralHttpError(statusCode, message, properties)
            };

        public static IDictionary<string, object> CreateProperties(string? correlationId = null)
        {
            var properties = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(correlationId))
            {
                properties.Add(WellKnownErrorProperty.CorrelationId, correlationId);
            }

            return properties;
        }
    }
}
