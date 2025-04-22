using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.Clients.Common.Constants
{
    [ExcludeFromCodeCoverage]
    public static class ApiHeaderKeys
    {
        public const string ErrorOrigin = "X-Error-Origin";
        public const string BearerTokenHeaderKey = "bearer";
        public const string XCorrelationIdHeaderKey = "X-Correlation-ID";
    }
}
