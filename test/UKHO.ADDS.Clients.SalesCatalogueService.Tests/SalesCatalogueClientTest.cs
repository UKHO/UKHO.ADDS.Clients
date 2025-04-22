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
        private object _responseBody;
        private DateTime _responseHeader;
        private HttpStatusCode _responseStatusCode;

        [SetUp]
        public void Setup()
        {
            _fakeScsHttpClientFactory = new FakeScsHttpClientFactory(request =>
            {
                return (_responseStatusCode, _responseBody, _responseHeader);
            });

            _responseBody = string.Empty;
            _responseStatusCode = HttpStatusCode.OK;
            _salesCatalogueApiClient = new SalesCatalogueClient(_fakeScsHttpClientFactory, @"https://scs-tests.net/basePath/", DUMMY_ACCESS_TOKEN);
        }

        [TearDown]
        public void TearDown()
        {
            _fakeScsHttpClientFactory.Dispose();
            if (_responseBody is IDisposable disposableResponse)
            {
                disposableResponse.Dispose();
            }
        }

        [Test]
        public async Task GetS100ProductsFromSpecificDateAsync_Success_ReturnsSuccessResult()
        {
            // Arrange
            var expectedProducts = new List<S100Products>
            {
                new S100Products { ProductName = "Product1", LatestEditionNumber = 1, LatestUpdateNumber = 1 },
                new S100Products { ProductName = "Product2", LatestEditionNumber = 2, LatestUpdateNumber = 2 }
            };

            var correlationId = Guid.NewGuid().ToString();
            var apiVersion = "v2";
            var productType = "s100";
            var sinceDateTime = DateTime.UtcNow.AddDays(-1);

            _responseBody = expectedProducts;
            _responseStatusCode = HttpStatusCode.OK;
            _responseHeader = sinceDateTime;

            // Act
            var result = await _salesCatalogueApiClient.GetS100ProductsFromSpecificDateAsync(apiVersion, productType, sinceDateTime, correlationId);

            // Assert
            result.IsSuccess(out S100SalesCatalogueResponse? s100SalesCatalogueResponse);
            CheckResponseMatchesExpectedResponse(s100SalesCatalogueResponse, result);
        }

        [Test]
        public async Task GetS100ProductsWithEmptySinceDateAsync_Success_ReturnsSuccessResult()
        {
            // Arrange
            var expectedProducts = new List<S100Products>
            {
                new S100Products { ProductName = "Product1", LatestEditionNumber = 1, LatestUpdateNumber = 1 },
                new S100Products { ProductName = "Product2", LatestEditionNumber = 2, LatestUpdateNumber = 2 }
            };

            var correlationId = Guid.NewGuid().ToString();
            var apiVersion = "v2";
            var productType = "s100";
            var sinceDateTime = DateTime.UtcNow.AddDays(-1);

            _responseBody = expectedProducts;
            _responseStatusCode = HttpStatusCode.OK;
            _responseHeader = sinceDateTime;

            // Act
            var result = await _salesCatalogueApiClient.GetS100ProductsFromSpecificDateAsync(apiVersion, productType, null, correlationId);

            // Assert
            result.IsSuccess(out S100SalesCatalogueResponse? s100SalesCatalogueResponse);
            CheckResponseMatchesExpectedResponse(s100SalesCatalogueResponse, result);
        }

        [Test]
        public async Task GetS100ProductsFromSpecificDateAsync_Failure_ReturnsFailureResult()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            var apiVersion = "v1";
            var productType = "s100";
            var sinceDateTime = DateTime.UtcNow.AddDays(-1);

            _responseBody = new StringContent("Error message");
            _responseStatusCode = HttpStatusCode.BadRequest;

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
            var sinceDateTime = DateTime.UtcNow.AddDays(-1);

            _responseBody = new StringContent("Response content");
            _responseStatusCode = HttpStatusCode.OK;

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
        private static void CheckResponseMatchesExpectedResponse(S100SalesCatalogueResponse? expectedResponse, IResult<S100SalesCatalogueResponse> response)
        {
            var isSuccess = response.IsSuccess(out var responseValue);

            Assert.That(responseValue!.ResponseBody.Count, Is.EqualTo(expectedResponse!.ResponseBody.Count));
            Assert.Multiple(() =>
            {
                Assert.That(responseValue.ResponseCode, Is.EqualTo(expectedResponse.ResponseCode));
                Assert.That(responseValue.LastModified, Is.EqualTo(expectedResponse.LastModified));
            });
        }
    }
}
