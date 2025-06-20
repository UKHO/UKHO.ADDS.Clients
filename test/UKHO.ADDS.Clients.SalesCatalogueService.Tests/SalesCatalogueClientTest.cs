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

        [TearDown]
        public void TearDown()
        {
            _fakeScsHttpClientFactory?.Dispose();
        }

        private void InitClient(
            HttpStatusCode statusCode,
            object responseBody,
            DateTime? contentLastModified = null,
            DateTime? headerLastModified = null)
        {
            _fakeScsHttpClientFactory = new FakeScsHttpClientFactory(_ =>
                (statusCode, responseBody, contentLastModified, headerLastModified)
            );
            _salesCatalogueApiClient = new SalesCatalogueClient(
                _fakeScsHttpClientFactory,
                @"https://scs-tests.net/basePath/",
                DUMMY_ACCESS_TOKEN
            );
        }

        [Test]
        public async Task GetS100ProductsFromSpecificDateAsync_Success_ReturnsSuccessResult()
        {
            var expectedProducts = new List<S100Products>
            {
                new() { ProductName = "Product1", LatestEditionNumber = 1, LatestUpdateNumber = 1 },
                new() { ProductName = "Product2", LatestEditionNumber = 2, LatestUpdateNumber = 2 }
            };
            var sinceDateTime = DateTime.UtcNow.AddDays(-1);

            InitClient(HttpStatusCode.OK, expectedProducts, sinceDateTime);

            var result = await _salesCatalogueApiClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", sinceDateTime, Guid.NewGuid().ToString());

            result.IsSuccess(out var s100SalesCatalogueResponse);
            CheckResponseMatchesExpectedResponse(s100SalesCatalogueResponse, result);
        }

        [Test]
        public async Task GetS100ProductsWithEmptySinceDateAsync_Success_ReturnsSuccessResult()
        {
            var expectedProducts = new List<S100Products>
            {
                new() { ProductName = "Product1", LatestEditionNumber = 1, LatestUpdateNumber = 1 },
                new() { ProductName = "Product2", LatestEditionNumber = 2, LatestUpdateNumber = 2 }
            };
            var sinceDateTime = DateTime.UtcNow.AddDays(-1);

            InitClient(HttpStatusCode.OK, expectedProducts, sinceDateTime);

            var result = await _salesCatalogueApiClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", null, Guid.NewGuid().ToString());

            result.IsSuccess(out S100SalesCatalogueResponse? s100SalesCatalogueResponse);
            CheckResponseMatchesExpectedResponse(s100SalesCatalogueResponse, result);
        }

        [Test]
        public async Task GetS100ProductsFromSpecificDateAsync_Failure_ReturnsFailureResult()
        {
            var errorContent = new StringContent("Error message");

            InitClient(HttpStatusCode.BadRequest, errorContent);

            var result = await _salesCatalogueApiClient.GetS100ProductsFromSpecificDateAsync(
                "v1", "s100", DateTime.UtcNow.AddDays(-1), Guid.NewGuid().ToString());

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
            var correlationId = Guid.NewGuid().ToString();
            var responseContent = new StringContent("Response content");

            InitClient(HttpStatusCode.OK, responseContent);

            await _salesCatalogueApiClient.GetS100ProductsFromSpecificDateAsync(
                "v1", "s100", DateTime.UtcNow.AddDays(-1), correlationId);

            var httpClient = _fakeScsHttpClientFactory.HttpClient;
            Assert.Multiple(() =>
            {
                Assert.That(httpClient.DefaultRequestHeaders.Contains(ApiHeaderKeys.XCorrelationIdHeaderKey), Is.True);
                Assert.That(httpClient.DefaultRequestHeaders.GetValues(ApiHeaderKeys.XCorrelationIdHeaderKey).FirstOrDefault(), Is.EqualTo(correlationId));
            });
        }

        [Test]
        public async Task GetS100ProductsFromSpecificDateAsync_LastModified_IgnoresMainHeader_UsesContentHeader()
        {
            var expectedProducts = new List<S100Products>
            {
                new() { ProductName = "Product1", LatestEditionNumber = 1, LatestUpdateNumber = 1 }
            };
            var contentLastModified = DateTime.UtcNow.AddDays(-2);
            var headerLastModified = DateTime.UtcNow.AddDays(-5);

            InitClient(HttpStatusCode.OK, expectedProducts, contentLastModified, headerLastModified);

            var result = await _salesCatalogueApiClient.GetS100ProductsFromSpecificDateAsync(
                "v2", "s100", DateTime.UtcNow.AddDays(-1), Guid.NewGuid().ToString());

            result.IsSuccess(out var s100SalesCatalogueResponse);
            Assert.That(s100SalesCatalogueResponse, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(s100SalesCatalogueResponse!.LastModified, Is.EqualTo(contentLastModified).Within(TimeSpan.FromSeconds(1)));
                Assert.That(s100SalesCatalogueResponse.LastModified, Is.Not.EqualTo(headerLastModified).Within(TimeSpan.FromSeconds(1)));
            });
        }

        private static void CheckResponseMatchesExpectedResponse(S100SalesCatalogueResponse? expectedResponse, IResult<S100SalesCatalogueResponse> response)
        {
            var isSuccess = response.IsSuccess(out var responseValue);

            Assert.That(responseValue!.ResponseBody, Has.Count.EqualTo(expectedResponse!.ResponseBody.Count));
            Assert.Multiple(() =>
            {
                Assert.That(responseValue.ResponseCode, Is.EqualTo(expectedResponse.ResponseCode));
                Assert.That(responseValue.LastModified, Is.EqualTo(expectedResponse.LastModified));
            });
        }
    }
}
