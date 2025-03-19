using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace UKHO.ADDS.Clients.PermitService.Models
{
    [ExcludeFromCodeCoverage]
    public class Product
    {
        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }

        [JsonPropertyName("editionNumber")]
        public int? EditionNumber { get; set; }

        [JsonPropertyName("permitExpiryDate")]
        public string? PermitExpiryDate { get; set; }
    }
}
