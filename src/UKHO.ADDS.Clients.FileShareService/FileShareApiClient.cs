using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using UKHO.ADDS.Clients.FileShareService.Authentication;
using UKHO.ADDS.Clients.FileShareService.Extensions;
using UKHO.ADDS.Clients.FileShareService.Factories;
using UKHO.ADDS.Clients.FileShareService.Models;

namespace UKHO.ADDS.Clients.FileShareService
{
    public interface IFileShareApiClient
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

    public class FileShareApiClient : IFileShareApiClient
    {
        private readonly int _maxDownloadBytes = 10485760;
        protected readonly IAuthenticationTokenProvider authTokenProvider;
        protected readonly IHttpClientFactory httpClientFactory;

        public FileShareApiClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider)
        {
            this.httpClientFactory = new SetBaseAddressHttpClientFactory(httpClientFactory, new Uri(baseAddress));
            this.authTokenProvider = authTokenProvider;
        }

        public FileShareApiClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken) :
            this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken))
        {
        }

        [ExcludeFromCodeCoverage] //This constructor is intended for external use with a real HTTP Client.
        public FileShareApiClient(string baseAddress, string accessToken) :
            this(new DefaultHttpClientFactory(), baseAddress, accessToken)
        {
        }

        public async Task<BatchStatusResponse> GetBatchStatusAsync(string batchId)
        {
            var uri = $"batch/{batchId}/status";

            using (var httpClient = await GetAuthenticationHeaderSetClient())
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                var response = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
                response.EnsureSuccessStatusCode();
                var status = await response.ReadAsTypeAsync<BatchStatusResponse>();
                return status;
            }
        }

        public async Task<BatchSearchResponse> SearchAsync(string searchQuery) => await SearchAsync(searchQuery, null, null);

        public async Task<BatchSearchResponse> SearchAsync(string searchQuery, int? pageSize) => await SearchAsync(searchQuery, pageSize, null);

        public async Task<BatchSearchResponse> SearchAsync(string searchQuery, int? pageSize, int? start)
        {
            var response = await SearchResponse(searchQuery, pageSize, start, CancellationToken.None);
            response.EnsureSuccessStatusCode();
            var searchResponse = await response.ReadAsTypeAsync<BatchSearchResponse>();
            return searchResponse;
        }

        public async Task<IResult<BatchSearchResponse>> SearchAsync(string searchQuery, int? pageSize, int? start, CancellationToken cancellationToken)
        {
            var response = await SearchResponse(searchQuery, pageSize, start, cancellationToken);
            return await Result.WithObjectData<BatchSearchResponse>(response);
        }

        public async Task<Stream> DownloadFileAsync(string batchId, string filename)
        {
            var uri = $"batch/{batchId}/files/{filename}";

            using (var httpClient = await GetAuthenticationHeaderSetClient())
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                var response = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
                response.EnsureSuccessStatusCode();
                var downloadedFileStream = await response.Content.ReadAsStreamAsync();
                return downloadedFileStream;
            }
        }

        public async Task<IResult<DownloadFileResponse>> DownloadFileAsync(string batchId, string fileName, Stream destinationStream, long fileSizeInBytes = 0, CancellationToken cancellationToken = default)
        {
            long startByte = 0;
            var endByte = fileSizeInBytes < _maxDownloadBytes ? fileSizeInBytes - 1 : _maxDownloadBytes - 1;
            IResult<DownloadFileResponse> result = null;

            while (startByte <= endByte)
            {
                var rangeHeader = $"bytes={startByte}-{endByte}";

                var uri = $"batch/{batchId}/files/{fileName}";

                using (var httpClient = await GetAuthenticationHeaderSetClient())
                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    if (fileSizeInBytes != 0 && rangeHeader != null)
                    {
                        httpRequestMessage.Headers.Add("Range", rangeHeader);
                    }

                    var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                    result = await Result.WithNullData<DownloadFileResponse>(response);

                    if (!result.IsSuccess)
                    {
                        return result;
                    }

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        contentStream.CopyTo(destinationStream);
                    }
                }

                startByte = endByte + 1;
                endByte += _maxDownloadBytes - 1;

                if (endByte > fileSizeInBytes - 1)
                {
                    endByte = fileSizeInBytes - 1;
                }
            }

            return result;
        }

        public async Task<IEnumerable<string>> GetUserAttributesAsync()
        {
            var uri = "attributes";

            using (var httpClient = await GetAuthenticationHeaderSetClient())
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                var response = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
                response.EnsureSuccessStatusCode();
                var attributes = await response.ReadAsTypeAsync<List<string>>();
                return attributes;
            }
        }

        public async Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, CancellationToken cancellationToken)
        {
            var uri = "attributes/search";

            var query = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query["$filter"] = searchQuery;
            }

            uri = AddQueryString(uri, query);

            using (var httpClient = await GetAuthenticationHeaderSetClient())
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                return await Result.WithObjectData<BatchAttributesSearchResponse>(response);
            }
        }

        public async Task<IResult<BatchAttributesSearchResponse>> BatchAttributeSearchAsync(string searchQuery, int maxAttributeValueCount, CancellationToken cancellationToken)
        {
            var uri = "attributes/search";

            var query = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query["$filter"] = searchQuery;
            }

            query["maxAttributeValueCount"] = maxAttributeValueCount.ToString();

            uri = AddQueryString(uri, query);

            using (var httpClient = await GetAuthenticationHeaderSetClient())
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                return await Result.WithObjectData<BatchAttributesSearchResponse>(response);
            }
        }

        public async Task<IResult<Stream>> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken)
        {
            var uri = $"batch/{batchId}/files";

            using (var httpClient = await GetAuthenticationHeaderSetClient())
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                var response = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
                return await Result.WithStreamData(response);
            }
        }

        protected async Task<HttpClient> GetAuthenticationHeaderSetClient()
        {
            var httpClient = httpClientFactory.CreateClient();
            await httpClient.SetAuthenticationHeaderAsync(authTokenProvider);
            return httpClient;
        }

        private async Task<HttpResponseMessage> SearchResponse(string searchQuery, int? pageSize, int? start, CancellationToken cancellationToken)
        {
            var uri = "batch";

            var query = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query["$filter"] = searchQuery;
            }

            if (pageSize.HasValue)
            {
                if (pageSize <= 0)
                {
                    throw new ArgumentException("Page size must be greater than zero.", nameof(pageSize));
                }

                query["limit"] = pageSize.Value + "";
            }

            if (start.HasValue)
            {
                if (start < 0)
                {
                    throw new ArgumentException("Start cannot be less than zero.", nameof(start));
                }

                query["start"] = start.Value + "";
            }

            uri = AddQueryString(uri, query);

            using (var httpClient = await GetAuthenticationHeaderSetClient())
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                return await httpClient.SendAsync(httpRequestMessage, cancellationToken);
            }
        }

        #region private methods

        private static string AddQueryString(string uri, IEnumerable<KeyValuePair<string, string>> queryString)
        {
            var uriToBeAppended = uri;

            var queryIndex = uriToBeAppended.IndexOf('?');
            var hasQuery = queryIndex != -1;

            var sb = new StringBuilder();
            sb.Append(uriToBeAppended);
            foreach (var parameter in queryString)
            {
                sb.Append(hasQuery ? '&' : '?');
                sb.Append(UrlEncoder.Default.Encode(parameter.Key));
                sb.Append('=');
                sb.Append(UrlEncoder.Default.Encode(parameter.Value));
                hasQuery = true;
            }

            return sb.ToString();
        }

        #endregion
    }
}
