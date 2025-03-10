﻿using System.Net;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.ADDS.Clients.FileShareService.Readonly.Tests.Helpers;

namespace UKHO.ADDS.Clients.FileShareService.Readonly.Tests
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

            var attributes = await _fileShareApiClient.GetUserAttributesAsync();

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo(""), "Should be no query query string for an empty search");
                Assert.That(attributes, Is.EqualTo((List<string>)_nextResponse));
            });
        }

        [Test]
        public async Task TestEmptyGetAttributes()
        {
            _nextResponse = new List<string>();

            var attributes = await _fileShareApiClient.GetUserAttributesAsync();

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo(""), "Should be no query query string for an empty search");
                Assert.That(attributes, Is.EqualTo((List<string>)_nextResponse));
            });
        }

        [Test]
        public void TestGetAttributesWhenServerReturnsError()
        {
            _nextResponseStatusCode = HttpStatusCode.ServiceUnavailable;

            var exception = Assert.ThrowsAsync<HttpRequestException>(_fileShareApiClient.GetUserAttributesAsync);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo("/basePath/attributes"));
                Assert.That(_lastRequestUri?.Query, Is.EqualTo(""), "Should be no query query string for an empty search");
                Assert.That(exception.Message, Is.EqualTo("Response status code does not indicate success: 503 (Service Unavailable)."));
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
