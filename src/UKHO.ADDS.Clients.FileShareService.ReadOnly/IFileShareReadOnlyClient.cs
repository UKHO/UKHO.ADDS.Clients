using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly
{
    public interface IFileShareReadOnlyClient
    {
        Task<IResult<BatchStatusResponse>> GetBatchStatusAsync(string batchId);
        Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, string correlationId);
        Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, string correlationId);
        Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start, string correlationId);
        Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start, string correlationId, CancellationToken cancellationToken);
        Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, CancellationToken cancellationToken);
        Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, int maxAttributeValueCount, CancellationToken cancellationToken);
        Task<IResult<Stream>> DownloadFileAsync(string batchId, string filename);
        Task<IResult<DownloadFileResponse>> DownloadFileAsync(string batchId, string fileName, Stream destinationStream, long fileSizeInBytes = 0, CancellationToken cancellationToken = default);
        Task<IResult<IEnumerable<string>>> GetUserAttributesAsync();
        Task<IResult<Stream>> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken);
    }
}
