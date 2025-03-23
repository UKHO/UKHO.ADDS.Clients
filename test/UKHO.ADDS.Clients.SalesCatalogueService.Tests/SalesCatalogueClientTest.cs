using System.Net;
using NUnit.Framework;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Tests.Helpers;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Tests
{
    public class SalesCatalogueClientTests
    {
        private const string DUMMY_ACCESS_TOKEN = "ACarefullyEncodedSecretAccessToken";
        private FakeScsHttpClientFactory _fakeScsHttpClientFactory;
        private SalesCatalogueClient _salesCatalogueApiClient;
        private Uri _lastRequestUri;
        private HttpContent _nextResponse;
        private HttpStatusCode _nextResponseStatusCode;

        [SetUp]
        public void Setup()
        {
            _fakeScsHttpClientFactory = new FakeScsHttpClientFactory(request =>
            {
                _lastRequestUri = request.RequestUri;
                return (_nextResponseStatusCode, _nextResponse);
            });

            _nextResponse = new StringContent(string.Empty);
            _nextResponseStatusCode = HttpStatusCode.OK;
            _salesCatalogueApiClient = new SalesCatalogueClient(_fakeScsHttpClientFactory, @"https://fss-tests.net/basePath/", DUMMY_ACCESS_TOKEN);
        }

        [TearDown]
        public void TearDown()
        {
            _fakeScsHttpClientFactory.Dispose();
            _nextResponse?.Dispose();
        }
        private static void CheckResponseMatchesExpectedResponse(S100SalesCatalogueResponse expectedResponse, IResult<S100SalesCatalogueResponse> response)
        {
            var isSuccess = response.IsSuccess(out var responseValue);

            Assert.That(responseValue.ResponseBody, Is.EqualTo(expectedResponse.ResponseBody));
            Assert.Multiple(() =>
            {
                Assert.That(responseValue.ResponseCode, Is.EqualTo(expectedResponse.ResponseCode));
                Assert.That(responseValue.LastModified, Is.EqualTo(expectedResponse.LastModified));
            });
        }

        //[Test]
        //public async Task GetS100ProductsFromSpecificDateAsync_Success_ReturnsSuccessResult()
        //{
        //    // Arrange
        //    var expectedProducts = new List<S100Products>
        //    {
        //        new S100Products { ProductName = "Product1", LatestEditionNumber = 1, LatestUpdateNumber = 1 },
        //        new S100Products { ProductName = "Product2", LatestEditionNumber = 2, LatestUpdateNumber = 2 }
        //    };
        //    var expectedResponse = new S100SalesCatalogueResponse
        //    {
        //        ResponseBody = expectedProducts,
        //        ResponseCode = HttpStatusCode.OK,
        //        LastModified = DateTime.UtcNow
        //    };
        //    var correlationId = Guid.NewGuid().ToString();
        //    var apiVersion = "v2";
        //    var productType = "s100";
        //    var sinceDateTime = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");

        //    _nextResponse = new StringContent(JsonSerializer.Serialize(expectedResponse), Encoding.UTF8, "application/json");
        //    _nextResponseStatusCode = HttpStatusCode.OK;

        //    // Act
        //    var result = await _salesCatalogueApiClient.GetS100ProductsFromSpecificDateAsync(apiVersion, productType, sinceDateTime, correlationId);

        //    // Assert
        //    CheckResponseMatchesExpectedResponse(expectedResponse, result);
        //}

        [Test]
        public async Task GetS100ProductsFromSpecificDateAsync_Failure_ReturnsFailureResult()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            var apiVersion = "v1";
            var productType = "s100";
            var sinceDateTime = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");

            _nextResponse = new StringContent("Error message");
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            // Act
            var result = await _salesCatalogueApiClient.GetS100ProductsFromSpecificDateAsync(apiVersion, productType, sinceDateTime, correlationId);

            // Assert
            var isSuccess = result.IsSuccess(out var value, out var error);
            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.False);
                Assert.That(value, Is.Null);
                Assert.That(error, Is.Not.Null);
            });
        }

        [Test]
        public async Task GetS100ProductsFromSpecificDateAsync_SetsCorrelationIdHeader()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            var apiVersion = "v1";
            var productType = "s100";
            var sinceDateTime = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");

            _nextResponse = new StringContent("Response content");
            _nextResponseStatusCode = HttpStatusCode.OK;

            // Act
            await _salesCatalogueApiClient.GetS100ProductsFromSpecificDateAsync(apiVersion, productType, sinceDateTime, correlationId);

            // Assert
            var httpClient = _fakeScsHttpClientFactory.HttpClient;
            Assert.Multiple(() =>
            {
                Assert.That(httpClient.DefaultRequestHeaders.Contains(ApiHeaderKeys.XCorrelationIdHeaderKey), Is.True);
                Assert.That(httpClient.DefaultRequestHeaders.GetValues(ApiHeaderKeys.XCorrelationIdHeaderKey).FirstOrDefault(), Is.EqualTo(correlationId));
            });
        }
    }
}
