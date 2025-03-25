using System.Diagnostics.CodeAnalysis;

namespace UKHO.ADDS.Clients.PermitService.Models
{
    [ExcludeFromCodeCoverage]
    public class UserPermit
    {
        public string? Title { get; set; }
        public string? Upn { get; set; }
    }
}
