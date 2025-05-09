using System.Net;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Models
{
    public record BaseResponse<T>(HttpStatusCode StatusCode, T Body) where T : class;
}
