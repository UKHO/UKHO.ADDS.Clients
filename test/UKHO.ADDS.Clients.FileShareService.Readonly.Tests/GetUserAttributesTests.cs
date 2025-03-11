using System.Net;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests.Helpers;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests
{
    public class GetUserAttributesTests
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
        public async Task TestSimpleGetAttributes()
        {
            _nextResponse = new List<string> { "One", "Two" };

            var attributesResult = await _fileShareApiClient.GetUserAttributesAsync();

            var isSuccess = attributesResult.IsSuccess(out var attributes);

            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.True);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo(""), "Should be no query query string for an empty search");
                Assert.That(attributes, Is.EqualTo((List<string>)_nextResponse));
            });
        }

        [Test]
        public async Task TestEmptyGetAttributes()
        {
            _nextResponse = new List<string>();

            var attributesResult = await _fileShareApiClient.GetUserAttributesAsync();

            attributesResult.IsSuccess(out var attributes);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo(""), "Should be no query query string for an empty search");
                Assert.That(attributes, Is.EqualTo((List<string>)_nextResponse));
            });
        }

        [Test]
        public async Task TestGetAttributesWhenServerReturnsError()
        {
            _nextResponseStatusCode = HttpStatusCode.ServiceUnavailable;

            var result = await _fileShareApiClient.GetUserAttributesAsync();

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo(""), "Should be no query query string for an empty search");
                Assert.That(result.IsFailure());
            });
        }

        [Test]
        public async Task TestGetAttributesSetsAuthorizationHeader()
        {
            _nextResponse = new List<string> { "One", "Two" };

            await _fileShareApiClient.GetUserAttributesAsync();

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo(DUMMY_ACCESS_TOKEN));
            });
        }
    }
}
