using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.Clients.Common.Constants
{
    [ExcludeFromCodeCoverage]
    public static class ApiHeaderKeys
    {
        public const string ErrorOrigin = "X-Error-Origin";
        public const string BearerTokenHeaderKey = "bearer";
        public const string XCorrelationIdHeaderKey = "X-Correlation-ID";
        public const string ContentType = "application/json";
        public const string MimeType = "X-MIME-Type";
        public const string ContentSize = "X-Content-Size";
        public const string ContentTypeOctetStream = "application/octet-stream";
    }
}
