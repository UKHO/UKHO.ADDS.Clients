using System.Net;
using FakeItEasy;
using NUnit.Framework;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests.Helpers;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests
{
    [TestFixture]
    internal class CreateBatchTest
    {
        private const string DummyAccessToken = "ACarefullyEncodedSecretAccessToken";
        private const string DummyCorrelationId = "dummy-correlation-id";
        private const string BaseAddress = "http://test.com";
        private const int MaxFileBlockSize = 8192;

        private FakeFssHttpClientFactory _fakeFssHttpClientFactory;
        private Uri _lastRequestUri;
        private object _nextResponse;
        private HttpStatusCode _nextResponseStatusCode;
        private IAuthenticationTokenProvider _fakeAuthTokenProvider;
        private FileShareReadWriteClient _fileShareReadWriteClient;
        private BatchModel _batchModel;


        [SetUp]
        public void Setup()
        {
            _fakeAuthTokenProvider = A.Fake<IAuthenticationTokenProvider>();
            A.CallTo(() => _fakeAuthTokenProvider.GetTokenAsync()).Returns(DummyAccessToken);
            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(request =>
            {
                _lastRequestUri = request.RequestUri;
                return (_nextResponseStatusCode, _nextResponse);
            });

            _batchModel = new BatchModel
            {
                BusinessUnit = "TestUnit",
                Acl = new Acl { ReadUsers = new[] { "user1" } },
                Attributes = new List<KeyValuePair<string, string>> { new("key", "value") },
                ExpiryDate = DateTime.UtcNow.AddDays(1)
            };

            _nextResponseStatusCode = HttpStatusCode.OK;
            _fileShareReadWriteClient = new FileShareReadWriteClient(_fakeFssHttpClientFactory, @"https://fss-tests.net/basePath/", DummyAccessToken);
        }

        [TearDown]
        public void TearDown()
        {
            _fakeFssHttpClientFactory.Dispose();
        }

        [Test]
        public void WhenHttpClientFactoryIsNull_ThenThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FileShareReadWriteClient(null, BaseAddress, DummyAccessToken));

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
                new FileShareReadWriteClient(_fakeFssHttpClientFactory, string.Empty, DummyAccessToken));

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
                new FileShareReadWriteClient(_fakeFssHttpClientFactory, "invalid_uri", DummyAccessToken));

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
                new FileShareReadWriteClient(_fakeFssHttpClientFactory, BaseAddress, (IAuthenticationTokenProvider)null));

            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.ParamName, Is.EqualTo("authTokenProvider"));
            });
        }

        [Test]
        public async Task WhenCreateBatchAsyncIsCalledWithValidBatchModel_ThenReturnSuccessResult()
        {
            var batchHandle = new BatchHandle("batchId");

            _nextResponse = batchHandle;
            _nextResponseStatusCode = HttpStatusCode.OK;

            var result = await _fileShareReadWriteClient.CreateBatchAsync(_batchModel, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess(out var value, out _), Is.True);
                Assert.That(value?.BatchId, Is.EqualTo(batchHandle.BatchId));
            });

            Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch"));
        }


        [Test]
        public async Task WhenCreateBatchAsyncIsCalledWithInvalidResponse_ThenReturnFailureResult()
        {
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            var result = await _fileShareReadWriteClient.CreateBatchAsync(_batchModel, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsFailure);
                Assert.That(result.Errors.FirstOrDefault()?.Message,
                    Is.EqualTo("Bad request"));
            });

            Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch"));
        }

        [Test]
        public async Task WhenCreateBatchAsyncThrowsException_ThenReturnFailureResult()
        {
            var exceptionMessage = "Test exception";
            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(_ =>
            {
                throw new Exception(exceptionMessage);
            });
            _fileShareReadWriteClient = new FileShareReadWriteClient(_fakeFssHttpClientFactory, @"https://fss-tests.net/basePath/", DummyAccessToken);

            var result = await _fileShareReadWriteClient.CreateBatchAsync(_batchModel, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo(exceptionMessage));
            });
        }

        [Test]
        public async Task WhenCreateHttpClientWithHeadersAsyncIsCalled_ThenSetsAuthenticationAndCorrelationIdHeaders()
        {
            var batchHandle = new BatchHandle("batchId");

            _nextResponse = batchHandle;
            _nextResponseStatusCode = HttpStatusCode.OK;

            var result = await _fileShareReadWriteClient.CreateBatchAsync(_batchModel, DummyCorrelationId, CancellationToken.None);

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
        public void WhenCreateHttpRequestMessageIsCalled_ThenHttpRequestMessageIsConfiguredCorrectly()
        {
            // Arrange
            var uri = new Uri("batch", UriKind.Relative);
            var batchModel = new BatchModel
            {
                BusinessUnit = "TestUnit",
                Acl = new Acl { ReadUsers = new[] { "user1" } },
                Attributes = new List<KeyValuePair<string, string>> { new("key", "value") },
                ExpiryDate = DateTime.UtcNow.AddDays(1)
            };

            // Use reflection to access the private CreateHttpRequestMessage method
            var method = typeof(FileShareReadWriteClient).GetMethod("CreateHttpRequestMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var httpRequestMessage = (HttpRequestMessage)method.Invoke(_fileShareReadWriteClient, new object[] { uri, batchModel });

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(httpRequestMessage!.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(httpRequestMessage.RequestUri, Is.EqualTo(uri));
                Assert.That(httpRequestMessage.Content, Is.Not.Null);

                var content = httpRequestMessage.Content!.ReadAsStringAsync().Result;
                var serializedBatchModel = JsonCodec.Encode(batchModel);
                Assert.That(content, Is.EqualTo(serializedBatchModel));

                Assert.That(httpRequestMessage.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));
            });
        }
    }
}
