using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly
{
    public interface IFileShareReadOnlyClient
    {
        Task<BatchStatusResponse> GetBatchStatusAsync(string batchId);
        Task<BatchSearchResponse> SearchAsync(string searchQuery);
        Task<BatchSearchResponse> SearchAsync(string searchQuery, int? pageSize);
        Task<BatchSearchResponse> SearchAsync(string searchQuery, int? pageSize, int? start);
        Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start, CancellationToken cancellationToken);
        Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, CancellationToken cancellationToken);
        Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, int maxAttributeValueCount, CancellationToken cancellationToken);
        Task<Stream> DownloadFileAsync(string batchId, string filename);
        Task<IResult<DownloadFileResponse>> DownloadFileAsync(string batchId, string fileName, Stream destinationStream, long fileSizeInBytes = 0, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetUserAttributesAsync();
        Task<IResult<Stream>> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken);
    }
}
