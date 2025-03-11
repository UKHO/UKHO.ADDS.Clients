using System.Net;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests.Helpers;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Results.Errors.Http;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests
{
    public class BatchAttributeSearchTests
    {
        private const string DUMMY_ACCESS_TOKEN = "ACarefullyEncodedSecretAccessToken";
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
            _fileShareApiClient = new FileShareReadOnlyClient(_fakeFssHttpClientFactory, @"https://fss-tests.net/basePath/", DUMMY_ACCESS_TOKEN);
        }

        [TearDown]
        public void TearDown() => _fakeFssHttpClientFactory.Dispose();

        [Test]
        public async Task TestEmptySearchQuery()
        {
            var firstAttributesList = new List<string> { "string1", "string2" };
            var secondAttributesList = new List<string> { "string3", "string4" };

            var batchAttributes = new List<BatchAttributesSearchAttribute> { new("Attribute1", firstAttributesList), new("Attribute2", secondAttributesList) };
            var expectedResponse = new BatchAttributesSearchResponse(2, batchAttributes);

            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.BatchAttributeSearchAsync("", CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes/search"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo(""), "Should be no query string for an empty search");
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task TestSimpleSearchString()
        {
            var firstAttributesList = new List<string> { "string1", "string2" };
            var secondAttributesList = new List<string> { "string3", "string4" };

            var batchAttributes = new List<BatchAttributesSearchAttribute> { new("Attribute1", firstAttributesList), new("Attribute2", secondAttributesList) };

            var expectedResponse = new BatchAttributesSearchResponse(2, batchAttributes);
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.BatchAttributeSearchAsync("$batch(key) eq 'value'", CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes/search"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task TestSimpleSearchWithNoResults()
        {
            var expectedResponse = new BatchAttributesSearchResponse(0, new List<BatchAttributesSearchAttribute>());
            _nextResponse = expectedResponse;

            var response = await _fileShareApiClient.BatchAttributeSearchAsync("$batch(key) eq 'value'", CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes/search"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
            });

            CheckResponseMatchesExpectedResponse(expectedResponse, response);
        }

        [Test]
        public async Task SearchQuerySetsAuthorizationHeader()
        {
            var expectedResponse = new BatchAttributesSearchResponse(0, new List<BatchAttributesSearchAttribute>());

            await _fileShareApiClient.BatchAttributeSearchAsync("", CancellationToken.None);

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo(DUMMY_ACCESS_TOKEN));
            });
        }

        [Test]
        public async Task TestSimpleSearchQueryForBadRequest()
        {
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            var response = await _fileShareApiClient.BatchAttributeSearchAsync("$batch(key) eq 'value'", CancellationToken.None);

            var isFailed = response.IsFailure(out var responseError);
            var error = responseError as HttpError;

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes/search"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
                Assert.That(error.StatusCode, Is.EqualTo(_nextResponseStatusCode));
                Assert.That(isFailed, Is.True);
            });
        }

        [Test]
        public async Task TestSimpleSearchQueryForInternalServerError()
        {
            _nextResponseStatusCode = HttpStatusCode.InternalServerError;

            var response = await _fileShareApiClient.BatchAttributeSearchAsync("$batch(key) eq 'value'", CancellationToken.None);

            var isFailed = response.IsFailure(out var responseError);
            var error = responseError as HttpError;

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes/search"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
                Assert.That(error.StatusCode, Is.EqualTo(_nextResponseStatusCode));
                Assert.That(isFailed, Is.True);
            });
        }

        [Test]
        public void TestNameOfAndAttributeValuesinToStringMethod()
        {
            var colourList = new List<string> { "red", "blue" };

            var searchBatchAttributes = new BatchAttributesSearchAttribute("Colour", colourList);
            var attributeValues = searchBatchAttributes.ToString();

            Assert.That(attributeValues, Is.EqualTo("class BatchAttributesSearchAttribute {\n Key: Colour\n Values: red, blue\n}\n"));
        }

        private static void CheckResponseMatchesExpectedResponse(BatchAttributesSearchResponse expectedResponse, IResult<BatchAttributesSearchResponse> response)
        {
            var isValid = response.IsSuccess(out var responseData);

            Assert.That(isValid, Is.True);

            Assert.That(responseData!.SearchBatchCount, Is.EqualTo(expectedResponse.SearchBatchCount));

            for (var i = 0; i < expectedResponse.BatchAttributes.Count; i++)
            {
                var expectedBatchAttribute = expectedResponse.BatchAttributes[i];
                var actualBatchAttribute = responseData.BatchAttributes[i];
                Assert.That(actualBatchAttribute.Key, Is.EqualTo(expectedBatchAttribute.Key));

                for (var j = 0; j < expectedBatchAttribute.Values.Count; j++)
                {
                    Assert.That(actualBatchAttribute.Values[j], Is.EqualTo(expectedBatchAttribute.Values[j]));
                }
            }
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(2)]
        [TestCase(10)]
        [TestCase(1000)]
        public async Task DoesBatchAttributeSearchReturnsSucessWithMaxAttributeValueCountandFilter(int maxAttributeValueCount)
        {
            var response = await _fileShareApiClient.BatchAttributeSearchAsync("$batch(key) eq 'value'", maxAttributeValueCount, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes/search"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27&maxAttributeValueCount=" + maxAttributeValueCount));
                Assert.That(response.IsSuccess, Is.True);
            });
        }

        [Test]
        public async Task DoesBatchAttributeSearchReturnsBadRequestWithMaxAttributeValueCountZeroAndFilter()
        {
            var maxAttributeValueCount = 0;
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            var response = await _fileShareApiClient.BatchAttributeSearchAsync("$batch(key) eq 'value'", maxAttributeValueCount, CancellationToken.None);

            var isFailed = response.IsFailure(out var responseError);
            var error = responseError as HttpError;

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes/search"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27&maxAttributeValueCount=0"));
                Assert.That(error.StatusCode, Is.EqualTo(_nextResponseStatusCode));
                Assert.That(isFailed, Is.True);
            });
        }
    }
}
