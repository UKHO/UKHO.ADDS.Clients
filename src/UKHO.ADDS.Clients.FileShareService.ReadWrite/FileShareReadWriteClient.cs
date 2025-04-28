using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite
{
    public class FileShareReadWriteClient : FileShareReadOnlyClient, IFileShareReadWriteClient
    {
        private const int DefaultMaxFileBlockSize = 4194304;
        private readonly int _maxFileBlockSize;

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken) : base(httpClientFactory, baseAddress, accessToken) => _maxFileBlockSize = DefaultMaxFileBlockSize;

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken, int maxFileBlockSize) : base(httpClientFactory, baseAddress, accessToken) => _maxFileBlockSize = maxFileBlockSize;

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider) : base(httpClientFactory, baseAddress, authTokenProvider) => _maxFileBlockSize = DefaultMaxFileBlockSize;

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider, int maxFileBlockSize) : base(httpClientFactory, baseAddress, authTokenProvider) => _maxFileBlockSize = maxFileBlockSize;

        public Task<IResult<AppendAclResponse>> AppendAclAsync(string batchId, Acl acl, CancellationToken cancellationToken = default) => Task.FromResult<IResult<AppendAclResponse>>(Result.Success(new AppendAclResponse()));

        public async Task<IResult<IBatchHandle>> CreateBatchAsync(BatchModel batchModel)
        {
            return await CreateBatchInternalAsync(batchModel, CancellationToken.None);
        }

        public async Task<IResult<IBatchHandle>> CreateBatchAsync(BatchModel batchModel, CancellationToken cancellationToken)
        {
            return await CreateBatchInternalAsync(batchModel, cancellationToken);
        }

        public Task<IResult<BatchStatusResponse>> GetBatchStatusAsync(IBatchHandle batchHandle) => Task.FromResult<IResult<BatchStatusResponse>>(Result.Success(new BatchStatusResponse()));

        public Task<IResult> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, params KeyValuePair<string, string>[] fileAttributes) => Task.FromResult<IResult>(Result.Success());

        public Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, CancellationToken cancellationToken, params KeyValuePair<string, string>[] fileAttributes) => Task.FromResult<IResult<AddFileToBatchResponse>>(Result.Success(new AddFileToBatchResponse()));

        public Task<IResult> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, Action<(int blocksComplete, int totalBlockCount)> progressUpdate, params KeyValuePair<string, string>[] fileAttributes) => Task.FromResult<IResult>(Result.Success());

        public Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, Action<(int blocksComplete, int totalBlockCount)> progressUpdate, CancellationToken cancellationToken, params KeyValuePair<string, string>[] fileAttributes) =>
            Task.FromResult<IResult<AddFileToBatchResponse>>(Result.Success(new AddFileToBatchResponse()));

        public Task<IResult> CommitBatchAsync(IBatchHandle batchHandle) => Task.FromResult<IResult>(Result.Success());

        public Task<IResult<CommitBatchResponse>> CommitBatchAsync(IBatchHandle batchHandle, CancellationToken cancellationToken) => Task.FromResult<IResult<CommitBatchResponse>>(Result.Success(new CommitBatchResponse()));

        public Task<IResult<ReplaceAclResponse>> ReplaceAclAsync(string batchId, Acl acl, CancellationToken cancellationToken = default) => Task.FromResult<IResult<ReplaceAclResponse>>(Result.Success(new ReplaceAclResponse()));

        public Task<IResult> RollBackBatchAsync(IBatchHandle batchHandle) => Task.FromResult<IResult>(Result.Success());

        public Task<IResult<RollBackBatchResponse>> RollBackBatchAsync(IBatchHandle batchHandle, CancellationToken cancellationToken) => Task.FromResult<IResult<RollBackBatchResponse>>(Result.Success(new RollBackBatchResponse()));

        public Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(string batchId, BatchExpiryModel batchExpiry, CancellationToken cancellationToken = default) => Task.FromResult<IResult<SetExpiryDateResponse>>(Result.Success(new SetExpiryDateResponse()));

        private async Task<IResult<IBatchHandle>> CreateBatchInternalAsync(BatchModel batchModel, CancellationToken cancellationToken)
        {
            var uri = new Uri($"batch", UriKind.Relative);

            try
            {
                using var httpClient = await GetAuthenticationHeaderSetClient();
                using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = new StringContent(JsonSerializer.Serialize(batchModel), Encoding.UTF8, "application/json")
                };

                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return Result.Failure<IBatchHandle>($"Failed to create batch. Status code: {response.StatusCode}");
                }

                var batchHandle = await response.Content.ReadFromJsonAsync<BatchHandle>(cancellationToken: cancellationToken);
                return Result.Success<IBatchHandle>(batchHandle);
            }
            catch (Exception ex)
            {
                return Result.Failure<IBatchHandle>(ex.Message);
            }
        }
    }
}
