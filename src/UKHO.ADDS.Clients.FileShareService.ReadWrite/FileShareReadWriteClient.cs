using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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

        public Task AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, params KeyValuePair<string, string>[] fileAttributes)
        {
            return AddFileToBatchAsync(batchHandle, stream, fileName, mimeType, fileAttributes);
        }

        public Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, CancellationToken cancellationToken, params KeyValuePair<string, string>[] fileAttributes)
        {
            return AddFileToBatchAsync(batchHandle, stream, fileName, mimeType, _ => { }, cancellationToken, fileAttributes);
        }

        public async Task AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, Action<(int blocksComplete, int totalBlockCount)> progressUpdate, params KeyValuePair<string, string>[] fileAttributes)
        {
            await AddFileAsync(batchHandle, stream, fileName, mimeType, progressUpdate, CancellationToken.None, fileAttributes);
        }

        public async Task<IResult<AddFileToBatchResponse>> AddFileToBatchAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType, Action<(int blocksComplete, int totalBlockCount)> progressUpdate, CancellationToken cancellationToken, params KeyValuePair<string, string>[] fileAttributes)
        {
            return await AddFiles(batchHandle, stream, fileName, mimeType, progressUpdate, cancellationToken, fileAttributes);
        }

        public Task<IResult> CommitBatchAsync(IBatchHandle batchHandle) => Task.FromResult<IResult>(Result.Success());

        public Task<IResult<CommitBatchResponse>> CommitBatchAsync(IBatchHandle batchHandle, CancellationToken cancellationToken) => Task.FromResult<IResult<CommitBatchResponse>>(Result.Success(new CommitBatchResponse()));

        public Task<IResult<ReplaceAclResponse>> ReplaceAclAsync(string batchId, Acl acl, CancellationToken cancellationToken = default) => Task.FromResult<IResult<ReplaceAclResponse>>(Result.Success(new ReplaceAclResponse()));

        public Task<IResult> RollBackBatchAsync(IBatchHandle batchHandle) => Task.FromResult<IResult>(Result.Success());

        public Task<IResult<RollBackBatchResponse>> RollBackBatchAsync(IBatchHandle batchHandle, CancellationToken cancellationToken) => Task.FromResult<IResult<RollBackBatchResponse>>(Result.Success(new RollBackBatchResponse()));

        public Task<IResult<SetExpiryDateResponse>> SetExpiryDateAsync(string batchId, BatchExpiryModel batchExpiry, CancellationToken cancellationToken = default) => Task.FromResult<IResult<SetExpiryDateResponse>>(Result.Success(new SetExpiryDateResponse()));

        private HttpRequestMessage CreateHttpRequestMessage(Uri uri, BatchModel batchModel)
        {
            return new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(JsonCodec.Encode(batchModel), Encoding.UTF8, "application/json")
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

        private async Task AddFileAsync(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
            Action<(int blocksComplete, int totalBlockCount)> progressUpdate, CancellationToken cancellationToken,
            params KeyValuePair<string, string>[] fileAttributes)
        {
            if (!stream.CanSeek)
                throw new ArgumentException("The stream must be seekable.", nameof(stream));
            stream.Seek(0, SeekOrigin.Begin);

            var fileUri = $"batch/{batchHandle.BatchId}/files/{fileName}";

            {
                var fileModel = new FileModel()
                { Attributes = fileAttributes ?? Enumerable.Empty<KeyValuePair<string, string>>() };

                var payloadJson = JsonConvert.SerializeObject(fileModel);

                using (var httpClient = await GetAuthenticationHeaderSetClient())
                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, fileUri) { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })
                {
                    httpRequestMessage.Headers.Add("X-Content-Size", "" + stream.Length);

                    if (!string.IsNullOrEmpty(mimeType)) httpRequestMessage.Headers.Add("X-MIME-Type", mimeType);

                    var createFileRecordResponse = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                    createFileRecordResponse.EnsureSuccessStatusCode();
                }
            }

            var fileBlocks = new List<string>();
            var fileBlockId = 0;
            var expectedTotalBlockCount = (int)Math.Ceiling(stream.Length / (double)_maxFileBlockSize);
            progressUpdate((0, expectedTotalBlockCount));

            var buffer = new byte[_maxFileBlockSize];

            using (var md5 = MD5.Create())
            using (var cryptoStream = new CryptoStream(stream, md5, CryptoStreamMode.Read, true))
            {
                while (true)
                {
                    fileBlockId++;
                    var ms = new MemoryStream();

                    var read = cryptoStream.Read(buffer, 0, _maxFileBlockSize);
                    if (read <= 0) break;
                    ms.Write(buffer, 0, read);

                    var fileBlockIdAsString = fileBlockId.ToString("D5");
                    var putFileUri = $"batch/{batchHandle.BatchId}/files/{fileName}/{fileBlockIdAsString}";
                    fileBlocks.Add(fileBlockIdAsString);
                    ms.Seek(0, SeekOrigin.Begin);

                    var blockMD5 = ms.CalculateMd5();

                    using (var httpClient = await GetAuthenticationHeaderSetClient())
                    using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, putFileUri) { Content = new StreamContent(ms) })
                    {
                        httpRequestMessage.Content.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                        httpRequestMessage.Content.Headers.ContentMD5 = blockMD5;

                        var putFileResponse = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                        putFileResponse.EnsureSuccessStatusCode();

                        progressUpdate((fileBlockId, expectedTotalBlockCount));
                    }
                }

                {
                    var writeBlockFileModel = new WriteBlockFileModel { BlockIds = fileBlocks };
                    var payloadJson = JsonConvert.SerializeObject(writeBlockFileModel);

                    using (var httpClient = await GetAuthenticationHeaderSetClient())
                    using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, fileUri) { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })
                    {
                        var writeFileResponse = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                        writeFileResponse.EnsureSuccessStatusCode();
                    }
                }

                ((BatchHandle)batchHandle).AddFile(fileName, Convert.ToBase64String(md5.Hash));
            }
        }
        private async Task<IResult<AddFileToBatchResponse>> AddFiles(IBatchHandle batchHandle, Stream stream, string fileName, string mimeType,
           Action<(int blocksComplete, int totalBlockCount)> progressUpdate, CancellationToken cancellationToken,
           params KeyValuePair<string, string>[] fileAttributes)
        {
            var mappedResult = new Result<AddFileToBatchResponse>();
            if (!stream.CanSeek)
                throw new ArgumentException("The stream must be seekable.", nameof(stream));
            stream.Seek(0, SeekOrigin.Begin);

            var fileUri = $"batch/{batchHandle.BatchId}/files/{fileName}";
            {
                var fileModel = new FileModel { Attributes = fileAttributes ?? Enumerable.Empty<KeyValuePair<string, string>>() };

                var requestHeaders = new Dictionary<string, string>
                {
                    { "X-Content-Size", "" + stream.Length }
                };

                if (!string.IsNullOrEmpty(mimeType)) requestHeaders.Add("X-MIME-Type", mimeType);

                var result = await SendResult<FileModel, AddFileToBatchResponse>(fileUri, HttpMethod.Post, fileModel, cancellationToken, requestHeaders);

                if (result.Errors != null && result.Errors.Any())
                {
                    mappedResult = (Result<AddFileToBatchResponse>)result;
                }
                else
                {
                    var fileBlocks = new List<string>();
                    var fileBlockId = 0;
                    var expectedTotalBlockCount = (int)Math.Ceiling(stream.Length / (double)_maxFileBlockSize);
                    progressUpdate((0, expectedTotalBlockCount));

                    var buffer = new byte[_maxFileBlockSize];

                    using (var md5 = MD5.Create())
                    using (var cryptoStream = new CryptoStream(stream, md5, CryptoStreamMode.Read, true))
                    {
                        while (true)
                        {
                            fileBlockId++;
                            var ms = new MemoryStream();

                            var read = cryptoStream.Read(buffer, 0, _maxFileBlockSize);
                            if (read <= 0) break;
                            ms.Write(buffer, 0, read);

                            var fileBlockIdAsString = fileBlockId.ToString("D5");
                            var putFileUri = $"batch/{batchHandle.BatchId}/files/{fileName}/{fileBlockIdAsString}";
                            fileBlocks.Add(fileBlockIdAsString);
                            ms.Seek(0, SeekOrigin.Begin);

                            var blockMD5 = ms.CalculateMd5();

                            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, putFileUri) { Content = new StreamContent(ms) })
                            {
                                httpRequestMessage.Content.Headers.ContentType =
                                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                                httpRequestMessage.Content.Headers.ContentMD5 = blockMD5;
                                progressUpdate((fileBlockId, expectedTotalBlockCount));

                                result = await SendMessageResult<AddFileToBatchResponse>(httpRequestMessage, cancellationToken);
                                if (result.Errors != null && result.Errors.Any())
                                {
                                    mappedResult = (Result<AddFileToBatchResponse>)result;
                                    break;
                                }
                            }
                        }

                        if (!(mappedResult.Errors != null && mappedResult.Errors.Any()))
                        {
                            var writeBlockFileModel = new WriteBlockFileModel { BlockIds = fileBlocks };
                            result = await SendResult<WriteBlockFileModel, AddFileToBatchResponse>(fileUri, HttpMethod.Put, writeBlockFileModel, cancellationToken);

                            if (result.Errors != null && result.Errors.Any())
                            {
                                mappedResult = (Result<AddFileToBatchResponse>)result;
                            }
                            else
                            {
                                ((BatchHandle)batchHandle).AddFile(fileName, Convert.ToBase64String(md5.Hash));
                                return result;
                            }
                        }
                    }
                }
            }

            return mappedResult;
        }
        private async Task<IResult<TResponse>> SendResult<TRequest, TResponse>(string uri, HttpMethod httpMethod, TRequest request, CancellationToken cancellationToken, Dictionary<string, string> requestHeaders = default)
            => await SendObjectResult<TResponse>(uri, httpMethod, request, cancellationToken, requestHeaders);

        private async Task<IResult<TResponse>> SendObjectResult<TResponse>(string uri, HttpMethod httpMethod, object request, CancellationToken cancellationToken, Dictionary<string, string> requestHeaders = default)
        {
            var payloadJson = JsonConvert.SerializeObject(request, new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffK" });
            var httpContent = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            using (var httpRequestMessage = new HttpRequestMessage(httpMethod, uri) { Content = httpContent })
            {
                foreach (var requestHeader in requestHeaders ?? new Dictionary<string, string>())
                {
                    httpRequestMessage.Headers.Add(requestHeader.Key, requestHeader.Value);
                }

                return await SendMessageResult<TResponse>(httpRequestMessage, cancellationToken);
            }
        }

        private async Task<IResult<TResponse>> SendMessageResult<TResponse>(HttpRequestMessage messageToSend, CancellationToken cancellationToken)
        {
            using (var httpClient = await GetAuthenticationHeaderSetClient())
            {
                var response = await httpClient.SendAsync(messageToSend, cancellationToken);
                var result = new Result<TResponse>();
                return await Result.WithObjectData<TResponse>(response);
            }
        }
    }
}
