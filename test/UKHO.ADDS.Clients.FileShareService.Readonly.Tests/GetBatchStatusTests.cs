using System.Net;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Models;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests.Helpers;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests
{
    public class GetBatchStatusTests
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
        public async Task TestBasicGetBatchStatus()
        {
            var batchId = "f382a514-aa1c-4709-aecd-ef06f1b963f5";
            var expectedBatchStatus = BatchStatusResponse.StatusEnum.Committed;
            _nextResponse = new BatchStatusResponse(batchId, expectedBatchStatus);

            var batchStatusResponse = await _fileShareApiClient.GetBatchStatusAsync(batchId);

            var isSuccess = batchStatusResponse.IsSuccess(out var batchResponseData);

            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.True);
                Assert.That(batchResponseData!.Status, Is.EqualTo(expectedBatchStatus));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/status"));
            });
        }

        [Test]
        public async Task TestGetBatchStatusForABatchThatDoesNotExist()
        {
            var batchId = Guid.NewGuid();
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            try
            {
                var result = await _fileShareApiClient.GetBatchStatusAsync(batchId.ToString());

                Assert.That(result.IsFailure);
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<HttpRequestException>());
            }

            Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/status"));
        }

        [Test]
        public async Task TestGetBatchStatusForABatchThatHasBeenDeleted()
        {
            var batchId = Guid.NewGuid();
            _nextResponseStatusCode = HttpStatusCode.Gone;

            try
            {
                var result = await _fileShareApiClient.GetBatchStatusAsync(batchId.ToString());

                Assert.That(result.IsFailure);
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<HttpRequestException>());
            }

            Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/status"));
        }

        [Test]
        public async Task TestGetBatchStatusSetsAuthorizationHeader()
        {
            var batchId = "f382a514-aa1c-4709-aecd-ef06f1b963f5";
            var expectedBatchStatus = BatchStatusResponse.StatusEnum.Committed;

            _nextResponse = new BatchStatusResponse(batchId, expectedBatchStatus);

            await _fileShareApiClient.GetBatchStatusAsync(batchId);

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo(DUMMY_ACCESS_TOKEN));
            });
        }
    }
}
