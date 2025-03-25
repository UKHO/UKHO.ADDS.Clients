using UKHO.ADDS.Clients.PermitService.Models;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.PermitService
{
    public interface IPermitClient
    {
        public Task<IResult<Stream>> GetPermitAsync(string apiVersion, string productType, PermitRequest requestBody,string correlationId);
    }
}
