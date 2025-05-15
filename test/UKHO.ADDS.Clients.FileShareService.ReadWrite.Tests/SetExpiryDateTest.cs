using System.Net;
using FakeItEasy;
using NUnit.Framework;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests.Helpers;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests
{
    [TestFixture]
    internal class SetExpiryDateTest
    {
        private const string DummyAccessToken = "ACarefullyEncodedSecretAccessToken";
        private const string DummyCorrelationId = "dummy-correlation-id";
        private const string BaseAddress = "https://fss-tests.net";
        private FakeFssHttpClientFactory _fakeFssHttpClientFactory;
        private Uri? _lastRequestUri;
        private readonly object _nextResponse = null!;
        private HttpStatusCode _nextResponseStatusCode;
        private IAuthenticationTokenProvider _fakeAuthTokenProvider;
        private FileShareReadWriteClient _fileShareReadWriteClient;
        private BatchExpiryModel _batchExpiry;
        private const string BatchId = "validBatchId";

        [SetUp]
        public void Setup()
        {
            _nextResponseStatusCode = HttpStatusCode.NoContent;

            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(request =>
            {
                _lastRequestUri = request.RequestUri;
                return (_nextResponseStatusCode, _nextResponse);
            });

            _fileShareReadWriteClient = new FileShareReadWriteClient(_fakeFssHttpClientFactory, $@"{BaseAddress}/basePath/", DummyAccessToken);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _batchExpiry = new BatchExpiryModel { ExpiryDate = DateTime.UtcNow.AddDays(1) };

            _fakeAuthTokenProvider = A.Fake<IAuthenticationTokenProvider>();
            A.CallTo(() => _fakeAuthTokenProvider.GetTokenAsync()).Returns(DummyAccessToken);
        }
        
        [Test]
        public async Task WhenSetExpiryDateAsyncCalledWithValidInputs_ThenReturnsSuccess()
        {
            var result = await _fileShareReadWriteClient.SetExpiryDateAsync(BatchId, _batchExpiry);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess(out var value, out _), Is.True);
                Assert.That(value.IsExpiryDateSet, Is.True);
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Contains(ApiHeaderKeys.XCorrelationIdHeaderKey), Is.False);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/validBatchId/expiry"));
            });
        }

        [TestCase(HttpStatusCode.BadRequest, "Bad request")]
        [TestCase(HttpStatusCode.InternalServerError, "An internal server error occured")]
        public async Task WhenSetExpiryDateAsyncCalledWithMultipleError_ThenReturnsFailure(HttpStatusCode httpStatusCode, string errorMessage)
        {
            _nextResponseStatusCode = httpStatusCode;

            var result = await _fileShareReadWriteClient.SetExpiryDateAsync(BatchId, _batchExpiry);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess(out var value, out var errors), Is.False);
                Assert.That(errors?.Message, Is.EqualTo(errorMessage));
            });
        }

        [Test]
        public async Task WhenSetExpiryDateAsyncCalledWithCorrelationId_ThenReturnsSuccess()
        {
            var result = await _fileShareReadWriteClient.SetExpiryDateAsync(BatchId, _batchExpiry, DummyCorrelationId);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess(out var value, out _), Is.True);
                Assert.That(value.IsExpiryDateSet, Is.True);
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Contains(ApiHeaderKeys.XCorrelationIdHeaderKey), Is.True);
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.GetValues(ApiHeaderKeys.XCorrelationIdHeaderKey), Contains.Item(DummyCorrelationId));
            });
        }

        [Test]
        public async Task WhenSetExpiryDateAsyncThrowsException_ThenReturnsFailure()
        {
            const string exceptionMessage = "Test exception";

            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(_ => throw new Exception(exceptionMessage));
            _fileShareReadWriteClient = new FileShareReadWriteClient(_fakeFssHttpClientFactory, $@"{BaseAddress}/basePath/", DummyAccessToken);

            var result = await _fileShareReadWriteClient.SetExpiryDateAsync(BatchId, _batchExpiry);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo(exceptionMessage));
            });
        }

        [Test]
        public void WhenFileShareReadWriteClientIsConstructedWithInvalidParameters_ThenThrowsArgumentException()
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentNullException>(() => new FileShareReadWriteClient(null, "https://valid-base-address.com", A.Fake<IAuthenticationTokenProvider>()));
                Assert.Throws<UriFormatException>(() => new FileShareReadWriteClient(A.Fake<IHttpClientFactory>(), string.Empty, A.Fake<IAuthenticationTokenProvider>()));
                Assert.Throws<UriFormatException>(() => new FileShareReadWriteClient(A.Fake<IHttpClientFactory>(), "invalid-uri", A.Fake<IAuthenticationTokenProvider>()));
            });
        }

        [TearDown]
        public void TearDown()
        {
            _fakeFssHttpClientFactory.Dispose();
        }
    }
}
