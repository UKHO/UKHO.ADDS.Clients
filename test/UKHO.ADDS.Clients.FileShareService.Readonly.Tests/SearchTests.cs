using System.Net;
using NUnit.Framework;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests.Helpers;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests
{
    public class SearchTests
    {
        private const string DummyAccessToken = "ACarefullyEncodedSecretAccessToken";
        private const string DummyCorrelationId = "dummy-correlation-id";
        private const string BaseAddress = "http://test.com";

        private FakeFssHttpClientFactory _fakeFssHttpClientFactory;
        private FileShareReadOnlyClient _fileShareApiClient;
        private Uri _lastRequestUri;
        private object _nextResponse;
        private HttpStatusCode _nextResponseStatusCode;

        [SetUp]
        public void Setup()
        {
            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(request =>
            {
                _lastRequestUri = request.RequestUri;
                return (_nextResponseStatusCode, _nextResponse);
            });

            _nextResponse = new object();
            _nextResponseStatusCode = HttpStatusCode.OK;
            _fileShareApiClient = new FileShareReadOnlyClient(_fakeFssHttpClientFactory, @"https://fss-tests.net/basePath/", DummyAccessToken);
        }

        [TearDown]
        public void TearDown() => _fakeFssHttpClientFactory.Dispose();

        [Test]
        public void WhenHttpClientFactoryIsNull_ThenThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FileShareReadOnlyClient(null, BaseAddress, DummyAccessToken));

            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.ParamName, Is.EqualTo("httpClientFactory"));
            });
        }

        [Test]
        public void WhenBaseAddressIsEmpty_ThenThrowsUriFormatException()
        {
            var exception = Assert.Throws<UriFormatException>(() =>
                new FileShareReadOnlyClient(_fakeFssHttpClientFactory, string.Empty, DummyAccessToken));

            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.Message, Is.EqualTo("Invalid URI: The URI is empty."));
            });
        }

        [Test]
        public void WhenBaseAddressIsInvalidUri_ThenThrowsUriFormatException()
        {
            var exception = Assert.Throws<UriFormatException>(() =>
                new FileShareReadOnlyClient(_fakeFssHttpClientFactory, "invalid_uri", DummyAccessToken));

            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.Message, Is.EqualTo("Invalid URI: The format of the URI could not be determined."));
            });
        }

        [Test]
        public void WhenAuthTokenProviderIsNull_ThenThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FileShareReadOnlyClient(_fakeFssHttpClientFactory, BaseAddress, (IAuthenticationTokenProvider)null));

            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.ParamName, Is.EqualTo("authTokenProvider"));
            });
        }

        private static void CheckResponseMatchesExpectedResponse(BatchSearchResponse expectedResponse, IResult<BatchSearchResponse> response)
        {
            var isSuccess = response.IsSuccess(out var responseValue);

            Assert.That(responseValue.Count, Is.EqualTo(expectedResponse.Count));
            Assert.Multiple(() =>
            {
                Assert.That(responseValue.Total, Is.EqualTo(expectedResponse.Total));
                Assert.That(responseValue.Links, Is.EqualTo(expectedResponse.Links));
                Assert.That(responseValue.Entries, Is.EqualTo(expectedResponse.Entries));
            });
        }

        [Test]
        public async Task TestEmptySearchQuery()
        {
            var expectedResponse = new BatchSearchResponse { Count = 2, Total = 2, Entries = new List<BatchDetails> { new("batch1"), new("batch2") }, Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("", DummyCorrelationId);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo(""), "Should be no query query string for an empty search");
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task TestSimpleSearchString()
        {
            var expectedResponse = new BatchSearchResponse { Count = 2, Total = 2, Entries = new List<BatchDetails> { new("batch1"), new("batch2") }, Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", DummyCorrelationId);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task TestSimpleSearchWithDifferentPageSize()
        {
            var expectedResponse = new BatchSearchResponse { Count = 2, Total = 2, Entries = new List<BatchDetails> { new("batch1"), new("batch2") }, Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", 50, DummyCorrelationId);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27&limit=50"));
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task TestSimpleSearchStartingOnNextPage()
        {
            var expectedResponse = new BatchSearchResponse { Count = 2, Total = 2, Entries = new List<BatchDetails> { new("batch1"), new("batch2") }, Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", null, 20, DummyCorrelationId);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27&start=20"));
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task TestSimpleSearchWithPageSizeAndStartingOnNextPage()
        {
            var expectedResponse = new BatchSearchResponse { Count = 2, Total = 2, Entries = new List<BatchDetails> { new("batch1"), new("batch2") }, Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", 10, 20, DummyCorrelationId);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27&limit=10&start=20"));
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [TestCase(-10)]
        [TestCase(0)]
        public void TestSearchWithInvalidPageSizeThrowsArgumentException(int pageSize)
        {
            var exception = Assert.ThrowsAsync<ArgumentException>(async () => await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", pageSize, 20, DummyCorrelationId));

            Assert.That(exception.Message, Is.EqualTo("Page size must be greater than zero. (Parameter 'pageSize')"));
            Assert.That(exception.ParamName, Is.EqualTo("pageSize"));
        }

        [Test]
        [TestCase(-10)]
        public void TestSearchWithInvalidPageStartThrowsArgumentException(int start)
        {
            var exception = Assert.ThrowsAsync<ArgumentException>(async () => await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", 10, start, DummyCorrelationId));

            Assert.That(exception.ParamName, Is.EqualTo("start"));
            Assert.That(exception.Message, Is.EqualTo("Start cannot be less than zero. (Parameter 'start')"));
        }

        [Test]
        public async Task TestSimpleSearchWithNoResults()
        {
            var expectedResponse = new BatchSearchResponse { Count = 0, Total = 0, Entries = new List<BatchDetails>(), Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", DummyCorrelationId);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task SearchQuerySetsAuthorizationHeader()
        {
            _nextResponse = new BatchSearchResponse { Count = 0, Total = 0, Entries = new List<BatchDetails>(), Links = new Links(new Link("self")) };

            await _fileShareApiClient.SearchAsync("", DummyCorrelationId);

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo(DummyAccessToken));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Contains("X-Correlation-ID"), Is.True);
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.GetValues("X-Correlation-ID"), Contains.Item(DummyCorrelationId));
            });
        }

        [Test]
        public async Task TestEmptySearchQueryWithCancellation()
        {
            var expectedResponse = new BatchSearchResponse { Count = 2, Total = 2, Entries = new List<BatchDetails> { new("batch1"), new("batch2") }, Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("", null, null, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo(""), "Should be no query query string for an empty search");
                //Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.True);
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task TestSimpleSearchStringWithCancellation()
        {
            var expectedResponse = new BatchSearchResponse { Count = 2, Total = 2, Entries = new List<BatchDetails> { new("batch1"), new("batch2") }, Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", null, null, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
                //Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.True);
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task TestSimpleSearchWithDifferentPageSizeAndCancellation()
        {
            var expectedResponse = new BatchSearchResponse { Count = 2, Total = 2, Entries = new List<BatchDetails> { new("batch1"), new("batch2") }, Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", 50, null, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27&limit=50"));
                //Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.True);
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task TestSimpleSearchStartingOnNextPageWithCancellation()
        {
            var expectedResponse = new BatchSearchResponse { Count = 2, Total = 2, Entries = new List<BatchDetails> { new("batch1"), new("batch2") }, Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", null, 20, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27&start=20"));
                //Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.True);
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [TestCase(-10)]
        [TestCase(0)]
        public void TestSearchWithInvalidPageSizeThrowsArgumentExceptionAndCancellationn(int pageSize)
        {
            var exception = Assert.ThrowsAsync<ArgumentException>(async () => await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", pageSize, 20, DummyCorrelationId, CancellationToken.None));

            Assert.That(exception.Message, Is.EqualTo("Page size must be greater than zero. (Parameter 'pageSize')"));
            Assert.That(exception.ParamName, Is.EqualTo("pageSize"));
        }

        [Test]
        public void TestSearchWithInvalidPageStartThrowsArgumentExceptionAndCancellation()
        {
            var exception = Assert.ThrowsAsync<ArgumentException>(async () => await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", -10, 20, DummyCorrelationId, CancellationToken.None));

            Assert.That(exception.Message, Is.EqualTo("Page size must be greater than zero. (Parameter 'pageSize')"));
            Assert.That(exception.ParamName, Is.EqualTo("pageSize"));
        }

        [Test]
        public async Task TestSimpleSearchWithNoResultsAndCancellation()
        {
            var expectedResponse = new BatchSearchResponse { Count = 0, Total = 0, Entries = new List<BatchDetails>(), Links = new Links(new Link("self")) };
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", null, null, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
                //Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.True);
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task SearchQuerySetsAuthorizationHeaderWithCancellation()
        {
            _nextResponse = new BatchSearchResponse { Count = 0, Total = 0, Entries = new List<BatchDetails>(), Links = new Links(new Link("self")) };

            await _fileShareApiClient.SearchAsync("", null, null, DummyCorrelationId, CancellationToken.None);

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo(DummyAccessToken));
            });
        }

        [Test]
        public async Task TestSimpleSearchQueryForBadRequestWithCancellation()
        {
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", null, null, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
                //Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.False);
            });
        }

        [Test]
        public async Task TestSimpleSearchQueryForInternalServerErrorWithCancellation()
        {
            _nextResponseStatusCode = HttpStatusCode.InternalServerError;

            var response = await _fileShareApiClient.SearchAsync("$batch(key) eq 'value'", null, null, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/batch"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
                //Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.False);
            });
        }
    }
}
