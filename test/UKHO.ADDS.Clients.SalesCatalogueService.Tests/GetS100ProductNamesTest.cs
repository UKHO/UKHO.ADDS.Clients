using System.Net;
using System.Text.Json;
using NUnit.Framework;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.Clients.SalesCatalogueService.Tests.Helpers;
using UKHO.ADDS.Infrastructure.Results;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Tests
{
    [TestFixture]
    public class GetS100ProductNamesTest
    {
        private const string DummyAccessToken = "ACarefullyEncodedSecretAccessToken";
        private const string TestProduct1 = "101GB007645NUTS57";
        private const string TestProduct2 = "101GB007645NUTS58";
        private const string BaseUri = "https://scs-tests.net/basePath/";
        private const string ProductEndpoint = "v2/products/s100/productNames";
        private const string CorrelationId = "Test_CorrelationId";

        private FakeScsHttpClientFactory _fakeScsHttpClientFactory;
        private SalesCatalogueClient _salesCatalogueApiClient;
        private object _responseBody;
        private HttpStatusCode _responseStatusCode;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _fakeScsHttpClientFactory = new FakeScsHttpClientFactory(request => (_responseStatusCode, _responseBody, null));

            _responseBody = string.Empty;
            _responseStatusCode = HttpStatusCode.OK;
            _salesCatalogueApiClient = new SalesCatalogueClient(_fakeScsHttpClientFactory, BaseUri, DummyAccessToken);
        }

        [Test]
        public async Task WhenRequestingS100ProductNames_ThenReturnsSuccessResult()
        {
            var productNames = new List<string> { TestProduct2, TestProduct1 };
            var expectedResponse = CreateSampleSuccessResponse();

            _responseBody = expectedResponse;
            _responseStatusCode = HttpStatusCode.OK;

            var result = await _salesCatalogueApiClient.GetS100ProductNamesAsync(productNames, CorrelationId);

            var isSuccess = result.IsSuccess(out var response, out var error);
            
            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.True);
                Assert.That(response, Is.Not.Null);
                Assert.That(error, Is.Null);
                
                Assert.That(response!.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response.Products.Count, Is.EqualTo(2));
                
                var counts = response.ProductCounts;
                Assert.That(counts.RequestedProductCount, Is.EqualTo(2));
                Assert.That(counts.ReturnedProductCount, Is.EqualTo(2));
                Assert.That(counts.RequestedProductsAlreadyUpToDateCount, Is.EqualTo(0));
                Assert.That(counts.RequestedProductsNotReturned, Is.Empty);
                
                ValidateProduct(
                    response.Products[0], 
                    TestProduct1, 
                    11, 
                    new[] { 0 }, 
                    3456, 
                    1);
                
                ValidateProduct(
                    response.Products[1], 
                    TestProduct2, 
                    5, 
                    new[] { 0, 1, 2 }, 
                    10368, 
                    3);
            });

            VerifyCorrelationId();
        }

        [Test]
        public async Task WhenRequestingS100ProductNamesWithEmptyList_ThenReturnsFailureResult()
        {
            var productNames = new List<string>();

            _responseBody = "Bad Request";
            _responseStatusCode = HttpStatusCode.BadRequest;

            var result = await _salesCatalogueApiClient.GetS100ProductNamesAsync(productNames, CorrelationId);
            AssertFailureResult(result);
        }

        [Test]
        public async Task WhenServerReturnsError_ThenReturnsFailureResult()
        {
            var productNames = new List<string> { TestProduct2, TestProduct1 };

            _responseBody = "Internal Server Error";
            _responseStatusCode = HttpStatusCode.InternalServerError;

            var result = await _salesCatalogueApiClient.GetS100ProductNamesAsync(productNames, CorrelationId);

            AssertFailureResult(result);
        }

        [Test]
        public async Task WhenResponseCannotBeDeserialized_ThenReturnsFailureResult()
        {
            var productNames = new List<string> { TestProduct2, TestProduct1 };

            _responseBody = "{ invalid json }";
            _responseStatusCode = HttpStatusCode.OK;

            var result = await _salesCatalogueApiClient.GetS100ProductNamesAsync(productNames, CorrelationId);

            AssertFailureResult(result);
        }

        [Test]
        public async Task WhenRequestingS100ProductNames_ThenSerializesPayloadCorrectly()
        {
            var productNames = new List<string> { TestProduct2, TestProduct1 };
            string capturedContent = null;

            var factory = new FakeScsHttpClientFactory(request =>
            {
                if (request.RequestUri!.ToString().EndsWith("productNames") && request.Method == HttpMethod.Post)
                {
                    capturedContent = request.Content!.ReadAsStringAsync().Result;
                }
                return (HttpStatusCode.OK, new S100ProductNamesResponse(), null);
            });

            var client = new SalesCatalogueClient(factory, BaseUri, DummyAccessToken);

            await client.GetS100ProductNamesAsync(productNames, CorrelationId);

            Assert.Multiple(() =>
            {
                Assert.That(capturedContent, Is.Not.Null, "Request content should be captured");
                
                var deserializedContent = JsonSerializer.Deserialize<List<string>>(capturedContent);
                Assert.That(deserializedContent, Is.EquivalentTo(productNames), "Request payload should match input product names");
            });
        }

        [Test]
        public async Task WhenRequestingS100ProductNames_ThenUsesCorrectEndpoint()
        {
            var productNames = new List<string> { TestProduct2 };
            Uri capturedUri = null;

            var factory = new FakeScsHttpClientFactory(request =>
            {
                capturedUri = request.RequestUri!;
                return (HttpStatusCode.OK, new S100ProductNamesResponse(), null);
            });

            var client = new SalesCatalogueClient(factory, BaseUri, DummyAccessToken);

            await client.GetS100ProductNamesAsync(productNames, CorrelationId);

            Assert.Multiple(() =>
            {
                Assert.That(capturedUri, Is.Not.Null, "Request URI should be captured");
                Assert.That(capturedUri.ToString(), Does.EndWith(ProductEndpoint), "Endpoint should match expected URI");
            });
        }

        private void AssertFailureResult<T>(IResult<T> result)
        {
            var isSuccess = result.IsSuccess(out var response, out var error);

            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.False, "Result should be a failure");
                Assert.That(response, Is.Null, "Response should be null for failure result");
                Assert.That(error, Is.Not.Null, "Error should not be null for failure result");
            });
        }
        
        private static S100ProductNamesResponse CreateSampleSuccessResponse()
        {
            return new S100ProductNamesResponse
            {
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 2,
                    ReturnedProductCount = 2,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = new List<RequestedProductsNotReturned>()
                },
                Products = new List<S100ProductNames>
                {
                    CreateSampleProduct(
                        TestProduct1, 
                        11, 
                        new[] { 0 }, 
                        3456, 
                        new[]
                        {
                            new S100ProductDate
                            {
                                IssueDate = DateTime.Parse("2024-07-16T18:20:30.4500000Z"),
                                UpdateApplicationDate = DateTime.Parse("2024-07-16T18:20:30.4500000Z"),
                                UpdateNumber = 0
                            }
                        }),
                    CreateSampleProduct(
                        TestProduct2, 
                        5, 
                        new[] { 0, 1, 2 }, 
                        10368, 
                        new[]
                        {
                            new S100ProductDate
                            {
                                IssueDate = DateTime.Parse("2024-07-16T18:20:30.4500000Z"),
                                UpdateApplicationDate = DateTime.Parse("2024-07-16T18:20:30.4500000Z"),
                                UpdateNumber = 0
                            },
                            new S100ProductDate
                            {
                                IssueDate = DateTime.Parse("2024-07-16T18:20:30.4500000Z"),
                                UpdateNumber = 1
                            },
                            new S100ProductDate
                            {
                                IssueDate = DateTime.Parse("2024-07-16T18:20:30.4500000Z"),
                                UpdateNumber = 2
                            }
                        })
                },
                ResponseCode = HttpStatusCode.OK
            };
        }
        
        private static S100ProductNames CreateSampleProduct(
            string productName, 
            int editionNumber, 
            int[] updateNumbers, 
            int fileSize, 
            S100ProductDate[] dates)
        {
            return new S100ProductNames
            {
                ProductName = productName,
                EditionNumber = editionNumber,
                UpdateNumbers = new List<int>(updateNumbers),
                FileSize = fileSize,
                Dates = new List<S100ProductDate>(dates)
            };
        }
        
        private static void ValidateProduct(
            S100ProductNames product, 
            string expectedName, 
            int expectedEdition,
            int[] expectedUpdateNumbers,
            int expectedFileSize,
            int expectedDatesCount)
        {
            Assert.That(product.ProductName, Is.EqualTo(expectedName), "Product name should match");
            Assert.That(product.EditionNumber, Is.EqualTo(expectedEdition), "Edition number should match");
            Assert.That(product.UpdateNumbers, Has.Count.EqualTo(expectedUpdateNumbers.Length), "Update numbers count should match");
            Assert.That(product.UpdateNumbers, Is.EquivalentTo(expectedUpdateNumbers), "Update numbers should match");
            Assert.That(product.FileSize, Is.EqualTo(expectedFileSize), "File size should match");
            Assert.That(product.Dates, Has.Count.EqualTo(expectedDatesCount), "Dates count should match");
        }
        
        private void VerifyCorrelationId()
        {
            var httpClient = _fakeScsHttpClientFactory.HttpClient;
            Assert.Multiple(() =>
            {
                Assert.That(httpClient.DefaultRequestHeaders.Contains(ApiHeaderKeys.XCorrelationIdHeaderKey), 
                    Is.True, "HTTP client should have correlation ID header");
                Assert.That(httpClient.DefaultRequestHeaders.GetValues(ApiHeaderKeys.XCorrelationIdHeaderKey).FirstOrDefault(), 
                    Is.EqualTo(CorrelationId), "Correlation ID should match expected value");
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_responseBody is IDisposable disposableResponse)
            {
                disposableResponse.Dispose();
            }
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _fakeScsHttpClientFactory?.Dispose();
        }
    }
}
