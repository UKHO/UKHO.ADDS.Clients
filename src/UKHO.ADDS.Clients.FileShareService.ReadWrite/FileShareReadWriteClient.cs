using System.Net;
using System.Net.Http.Json;
using System.Text;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.Clients.Common.Factories;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite
{
    public class FileShareReadWriteClient : FileShareReadOnlyClient, IFileShareReadWriteClient
    {
        private const int DefaultMaxFileBlockSize = 4194304;
        private readonly int _maxFileBlockSize;

        private readonly IAuthenticationTokenProvider _authTokenProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider)
            : base(httpClientFactory, baseAddress, authTokenProvider)
        {
            if (httpClientFactory == null)
                throw new ArgumentNullException(nameof(httpClientFactory));
            if (string.IsNullOrWhiteSpace(baseAddress))
                throw new UriFormatException(nameof(baseAddress));
            if (!Uri.IsWellFormedUriString(baseAddress, UriKind.Absolute))
                throw new UriFormatException(nameof(baseAddress));

            _httpClientFactory = new SetBaseAddressHttpClientFactory(httpClientFactory, new Uri(baseAddress));
            _authTokenProvider = authTokenProvider ?? throw new ArgumentNullException(nameof(authTokenProvider));
            _maxFileBlockSize = DefaultMaxFileBlockSize;
        }

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken) :
            this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken)) => _maxFileBlockSize = DefaultMaxFileBlockSize;

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken, int maxFileBlockSize) : this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken)) => _maxFileBlockSize = maxFileBlockSize;

        public FileShareReadWriteClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider, int maxFileBlockSize) : this(httpClientFactory, baseAddress, authTokenProvider) => _maxFileBlockSize = maxFileBlockSize;

        public Task<IResult<AppendAclResponse>> AppendAclAsync(string batchId, Acl acl, CancellationToken cancellationToken = default) => Task.FromResult<IResult<AppendAclResponse>>(Result.Success(new AppendAclResponse()));

        public async Task<IResult<IBatchHandle>> CreateBatchAsync(BatchModel batchModel, CancellationToken cancellationToken = default)
        {
            return await CreateBatchInternalAsync(batchModel, cancellationToken);
        }

        public async Task<IResult<IBatchHandle>> CreateBatchAsync(BatchModel batchModel, string correlationId, CancellationToken cancellationToken = default)
        {
            return await CreateBatchInternalAsync(batchModel, cancellationToken, correlationId);
        }

        private async Task<IResult<IBatchHandle>> CreateBatchInternalAsync(BatchModel batchModel, CancellationToken cancellationToken, string? correlationId = null)
        {
            var uri = new Uri("batch", UriKind.Relative);

            try
            {
                using var httpClient = await CreateHttpClientWithHeadersAsync(correlationId);

                var httpRequestMessage = CreateHttpRequestMessage(uri, batchModel);

                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMetadata = await response.CreateErrorMetadata(ApiNames.FileShareService, correlationId);
                    return Result.Failure<IBatchHandle>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
                }

                var batchHandle = await response.Content.ReadFromJsonAsync<BatchHandle>(cancellationToken: cancellationToken);
                return Result.Success<IBatchHandle>(batchHandle);
            }
            catch (Exception ex)
            {
                return Result.Failure<IBatchHandle>(ex.Message);
            }
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

        public async Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(string batchId, BatchExpiryModel batchExpiry, CancellationToken cancellationToken = default)
        {
            return await SetExpiryDateInternalAsync(batchId, batchExpiry, cancellationToken);
        }

        public async Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(string batchId, BatchExpiryModel batchExpiry, string correlationId, CancellationToken cancellationToken = default)
        {
            return await SetExpiryDateInternalAsync(batchId, batchExpiry, cancellationToken ,correlationId);
        }

        private async Task<IResult<SetExpiryDateResponse>> SetExpiryDateInternalAsync(string batchId, BatchExpiryModel batchExpiryModel, CancellationToken cancellationToken, string? correlationId = null)
        {
            var uri = new Uri($"batch/{batchId}/expiry", UriKind.Relative);

            try
            {
                using var httpClient = await CreateHttpClientWithHeadersAsync(correlationId);

                var httpRequestMessage = CreateHttpRequestMessageForSetExpiryDate(uri, batchExpiryModel);

                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return Result.Success(new SetExpiryDateResponse() { IsExpiryDateSet = true });
                }

                var errorMetadata = await response.CreateErrorMetadata(ApiNames.FileShareService, correlationId);
                return Result.Failure<SetExpiryDateResponse>(ErrorFactory.CreateError(response.StatusCode, errorMetadata));
            }
            catch (Exception ex)
            {
                return Result.Failure<SetExpiryDateResponse>(ex.Message);
            }
        }

        private HttpRequestMessage CreateHttpRequestMessage(Uri uri, BatchModel batchModel)
        {
            return new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(JsonCodec.Encode(batchModel), Encoding.UTF8, "application/json")
            };
        }

        private HttpRequestMessage CreateHttpRequestMessageForSetExpiryDate(Uri uri, BatchExpiryModel batchExpiryModel)
        {
            var formattedExpiryDate = batchExpiryModel.ExpiryDate?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            return new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = new StringContent(JsonCodec.Encode(new { ExpiryDate = formattedExpiryDate }), Encoding.UTF8, "application/json")
            };
        }

        protected async Task<HttpClient> CreateHttpClientWithHeadersAsync(string? correlationId = null)
        {
            var httpClient = _httpClientFactory.CreateClient();
            await httpClient.SetAuthenticationHeaderAsync(_authTokenProvider);
            if (!string.IsNullOrEmpty(correlationId))
            {
                httpClient.SetCorrelationIdHeader(correlationId);
            }
            return httpClient;
        }
    }
}
