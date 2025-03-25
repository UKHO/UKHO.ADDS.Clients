using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;


namespace UKHO.ADDS.Clients.PermitService.Models
{
    [ExcludeFromCodeCoverage]
    public class PermitRequest
    {
        [JsonPropertyName("products")]
        public IEnumerable<Product>? Products { get; set; }

        [JsonPropertyName("userPermits")]
        public IEnumerable<UserPermit>? UserPermits { get; set; }
    }
}
