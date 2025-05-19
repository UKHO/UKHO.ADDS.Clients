using System.Net;
using System.Text;
using FakeItEasy;
using NUnit.Framework;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models.Response;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests.Helpers;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests
{
    [TestFixture]
    internal class CommitBatchTest
    {
        private const string DummyAccessToken = "ACarefullyEncodedSecretAccessToken";
        private const string DummyCorrelationId = "dummy-correlation-id";
        private const string DummyBatchId = "dummy-batch-id";
        private const string BaseAddress = "https://fss-tests.net";

        private FakeFssHttpClientFactory _fakeFssHttpClientFactory;
        private IBatchHandle _batchHandle;
        private Uri _lastRequestUri;
        private object _nextResponse;
        private HttpStatusCode _nextResponseStatusCode;
        private IAuthenticationTokenProvider _fakeAuthTokenProvider;
        private FileShareReadWriteClient _fileShareReadWriteClient;

        [SetUp]
        public void Setup()
        {
            _batchHandle = A.Fake<IBatchHandle>();
            _fakeAuthTokenProvider = A.Fake<IAuthenticationTokenProvider>();
            A.CallTo(() => _batchHandle.BatchId).Returns(DummyBatchId);
            A.CallTo(() => _fakeAuthTokenProvider.GetTokenAsync()).Returns(DummyAccessToken);

            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(request =>
            {
                _lastRequestUri = request.RequestUri;
                return (_nextResponseStatusCode, _nextResponse);
            });

            _nextResponseStatusCode = HttpStatusCode.OK;
            _fileShareReadWriteClient = new FileShareReadWriteClient(_fakeFssHttpClientFactory, $@"{BaseAddress}/basePath/", DummyAccessToken);
        }
        
        [Test]
        public void WhenHttpClientFactoryIsNull_ThenThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new FileShareReadWriteClient(null!, BaseAddress, DummyAccessToken));

            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.Message, Does.Contain("Value cannot be null. (Parameter 'httpClientFactory"));
                Assert.That(exception.ParamName, Is.EqualTo("httpClientFactory"));
            });
        }

        [Test]
        public void WhenBaseAddressIsEmpty_ThenThrowsUriFormatException()
        {
            var exception = Assert.Throws<UriFormatException>(() => new FileShareReadWriteClient(_fakeFssHttpClientFactory, string.Empty, DummyAccessToken));

            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.Message, Is.EqualTo("Invalid URI: The URI is empty."));
            });
        }

        [Test]
        public void WhenBaseAddressIsInvalidUri_ThenThrowsUriFormatException()
        {
            var exception = Assert.Throws<UriFormatException>(() => new FileShareReadWriteClient(_fakeFssHttpClientFactory, "invalid_uri", DummyAccessToken));

            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.Message, Is.EqualTo("Invalid URI: The format of the URI could not be determined."));
            });
        }

        [Test]
        public void WhenAuthTokenProviderIsNull_ThenThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new FileShareReadWriteClient(_fakeFssHttpClientFactory, BaseAddress, (IAuthenticationTokenProvider)null));

            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.ParamName, Is.EqualTo("authTokenProvider"));
            });
        }

        [Test]
        public async Task WhenCommitBatchAsyncIsCalledWithValidBatchHandleAndCorrelationId_ThenReturnsSuccessResult()
        {
            var expectedCommitBatchResponse = new CommitBatchResponse
            {
                Status = new CommitBatchStatus { Uri = DummyBatchId }
            };

            _nextResponse = expectedCommitBatchResponse;
            _nextResponseStatusCode = HttpStatusCode.Accepted;

            var result = await _fileShareReadWriteClient.CommitBatchAsync(_batchHandle, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess(out var value, out _), Is.True);
                Assert.That(value, Is.Not.Null);
                Assert.That(value!.Status, Is.Not.Null);
                Assert.That(value.Status.Uri, Is.EqualTo(DummyBatchId));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Contains(ApiHeaderKeys.XCorrelationIdHeaderKey), Is.True);
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.GetValues(ApiHeaderKeys.XCorrelationIdHeaderKey), Contains.Item(DummyCorrelationId));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{DummyBatchId}"));
            });
        }

        [Test]
        public async Task WhenCommitBatchAsyncIsCalledWithValidBatchHandleAndWithoutCorrelationIdAndCancellationToken_ThenReturnsSuccessResult()
        {
            var expectedCommitBatchResponse = new CommitBatchResponse
            {
                Status = new CommitBatchStatus { Uri = DummyBatchId }
            };

            _nextResponse = expectedCommitBatchResponse;
            _nextResponseStatusCode = HttpStatusCode.Accepted;

            var result = await _fileShareReadWriteClient.CommitBatchAsync(_batchHandle);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess(out var value, out _), Is.True);
                Assert.That(value, Is.Not.Null);
                Assert.That(value!.Status, Is.Not.Null);
                Assert.That(value.Status.Uri, Is.EqualTo(DummyBatchId));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{DummyBatchId}"));
            });
        }

        [Test]
        public async Task WhenCommitBatchAsyncIsCalledWithInvalidResponseAndCorrelationIdAndCancellationToken_ThenReturnFailureResult()
        {
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            var result = await _fileShareReadWriteClient.CommitBatchAsync(_batchHandle, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsFailure);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Bad request"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Contains(ApiHeaderKeys.XCorrelationIdHeaderKey), Is.True);
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.GetValues(ApiHeaderKeys.XCorrelationIdHeaderKey), Contains.Item(DummyCorrelationId));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{DummyBatchId}"));
            });
        }

        [Test]
        public async Task WhenCommitBatchAsyncIsCalledWithInvalidResponseAndWithoutCorrelationIdAndCancellationToken_ThenReturnFailureResult()
        {
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            var result = await _fileShareReadWriteClient.CommitBatchAsync(_batchHandle);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsFailure);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Bad request"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Contains(ApiHeaderKeys.XCorrelationIdHeaderKey), Is.False);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{DummyBatchId}"));
            });
        }

        [Test]
        public async Task WhenCommitBatchAsyncThrowsException_ThenReturnFailureResult()
        {
            var exceptionMessage = "Test exception";
            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(_ => throw new Exception(exceptionMessage));
            _fileShareReadWriteClient = new FileShareReadWriteClient(_fakeFssHttpClientFactory, $@"{BaseAddress}/basePath/", DummyAccessToken);

            var result = await _fileShareReadWriteClient.CommitBatchAsync(_batchHandle, DummyCorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo(exceptionMessage));
            });
        }

        [Test]
        public async Task WhenCommitBatchAsyncWithoutCorrelationIdAndCancellationTokenThrowsException_ThenReturnFailureResult()
        {
            var exceptionMessage = "Test exception";
            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(_ => throw new Exception(exceptionMessage));
            _fileShareReadWriteClient = new FileShareReadWriteClient(_fakeFssHttpClientFactory, $@"{BaseAddress}/basePath/", DummyAccessToken);

            var result = await _fileShareReadWriteClient.CommitBatchAsync(_batchHandle);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo(exceptionMessage));
            });
        }

        [TearDown]
        public void TearDown()
        {
            _fakeFssHttpClientFactory.Dispose();
        }
    }
}
