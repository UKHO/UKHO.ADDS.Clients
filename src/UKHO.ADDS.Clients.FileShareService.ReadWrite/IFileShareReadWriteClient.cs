using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite
{
    public interface IFileShareReadWriteClient : IFileShareReadOnlyClient
    {
        Task<IResult<AppendAclResponse>> AppendAclAsync(string batchId, Acl acl, CancellationToken cancellationToken = default);
        Task<IResult<IBatchHandle>> CreateBatchAsync(BatchModel batchModel);
        Task<IResult<IBatchHandle>> CreateBatchAsync(BatchModel batchModel, CancellationToken cancellationToken);
        Task<IResult<BatchStatusResponse>> GetBatchStatusAsync(IBatchHandle batchHandle);

        Task<IResult> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
            params KeyValuePair<string, string>[] fileAttributes);

        Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
            CancellationToken cancellationToken, params KeyValuePair<string, string>[] fileAttributes);

        Task<IResult> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
            Action<(int blocksComplete, int totalBlockCount)> progressUpdate, params KeyValuePair<string, string>[] fileAttributes);

        Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
            Action<(int blocksComplete, int totalBlockCount)> progressUpdate, CancellationToken cancellationToken,
            params KeyValuePair<string, string>[] fileAttributes);

        Task<IResult> CommitBatchAsync(IBatchHandle batchHandle);
        Task<IResult<CommitBatchResponse>> CommitBatchAsync(IBatchHandle batchHandle, CancellationToken cancellationToken);
        Task<IResult<ReplaceAclResponse>> ReplaceAclAsync(string batchId, Acl acl, CancellationToken cancellationToken = default);
        Task<IResult> RollBackBatchAsync(IBatchHandle batchHandle);
        Task<IResult<RollBackBatchResponse>> RollBackBatchAsync(IBatchHandle batchHandle, CancellationToken cancellationToken);
        Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(string batchId, BatchExpiryModel batchExpiry, CancellationToken cancellationToken = default);
    }
}
