using System.Net;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.Readonly.Tests.Helpers;

namespace UKHO.ADDS.Clients.FileShareService.Readonly.Tests
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

            CheckResponseMatchesExpectedResponse(expectedResponse, response.Data);
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

            CheckResponseMatchesExpectedResponse(expectedResponse, response.Data);
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

            CheckResponseMatchesExpectedResponse(expectedResponse, response.Data);
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

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes/search"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
                Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.False);
            });
        }

        [Test]
        public async Task TestSimpleSearchQueryForInternalServerError()
        {
            _nextResponseStatusCode = HttpStatusCode.InternalServerError;

            var response = await _fileShareApiClient.BatchAttributeSearchAsync("$batch(key) eq 'value'", CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes/search"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27"));
                Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.False);
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

        #region Private method

        private static void CheckResponseMatchesExpectedResponse(BatchAttributesSearchResponse expectedResponse, BatchAttributesSearchResponse response)
        {
            Assert.That(response.SearchBatchCount, Is.EqualTo(expectedResponse.SearchBatchCount));

            for (var i = 0; i < expectedResponse.BatchAttributes.Count; i++)
            {
                var expectedBatchAttribute = expectedResponse.BatchAttributes[i];
                var actualBatchAttribute = response.BatchAttributes[i];
                Assert.That(actualBatchAttribute.Key, Is.EqualTo(expectedBatchAttribute.Key));

                for (var j = 0; j < expectedBatchAttribute.Values.Count; j++)
                {
                    Assert.That(actualBatchAttribute.Values[j], Is.EqualTo(expectedBatchAttribute.Values[j]));
                }
            }
        }

        #endregion

        #region BatchSearch with MaxAttributeValueCount

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
        public async Task DoesBatchAttributeSearchReturnsBadRequestWithMaxAttributeValueCountZeroandFilter()
        {
            var MaxAttributeValueCount = 0;
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            var response = await _fileShareApiClient.BatchAttributeSearchAsync("$batch(key) eq 'value'", MaxAttributeValueCount, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes/search"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo("?$filter=$batch(key)%20eq%20%27value%27&maxAttributeValueCount=0"));
                Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.False);
            });
        }

        #endregion
    }
}
