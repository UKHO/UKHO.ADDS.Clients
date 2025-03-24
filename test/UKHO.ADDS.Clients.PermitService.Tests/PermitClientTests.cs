using System.Collections.Concurrent;
using System.Net;
using System.Text;
using NUnit.Framework;
using UKHO.ADDS.Clients.PermitService.Models;
using UKHO.ADDS.Clients.PermitService.Tests.Helpers;

namespace UKHO.ADDS.Clients.PermitService.Tests
{
    public class PermitClientTests
    {
        private const string DUMMY_ACCESS_TOKEN = "ACarefullyEncodedSecretAccessToken";
        private const string XCorrelationIdHeaderKey = "X-Correlation-ID";
        private FakePermitHttpClientFactory _fakePermitHttpClientFactory;
        private PermitClient _permitApiClient;
        private Uri _lastRequestUri;
        private ConcurrentQueue<object> _responseBody;
        private HttpStatusCode _responseStatusCode;

        [SetUp]
        public void Setup()
        {
            _fakePermitHttpClientFactory = new FakePermitHttpClientFactory(request =>
            {
                _lastRequestUri = request.RequestUri;

                if (_responseBody.IsEmpty)
                {
                    return (_responseStatusCode, new object());
                }

                if (_responseBody.TryDequeue(out var response))
                {
                    return (_responseStatusCode, response);
                }

                throw new Exception("Failed to dequeue next response");
            });

            _responseBody = new ConcurrentQueue<object>();
            _responseStatusCode = HttpStatusCode.OK;
            _permitApiClient = new PermitClient(_fakePermitHttpClientFactory, @"https://permit-tests.net/", DUMMY_ACCESS_TOKEN);
        }

        [TearDown]
        public void TearDown() => _fakePermitHttpClientFactory.Dispose();

        [Test]
        public async Task GetPermitAsync_Success_ReturnsSuccessResult()
        {
            var correlationId = Guid.NewGuid().ToString();
            var apiVersion = "v1";
            var productType = "s100";
            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(GetExpectedXmlString()));
            _responseBody.Enqueue(expectedStream);

            var permitRequest = new PermitRequest
            {
                Products = new List<Product>
                {
                    new() {
                        ProductName = "Product1",
                        EditionNumber = 1,
                        PermitExpiryDate = DateTime.UtcNow.AddDays(1).ToString("YYYY-MM-DD")
                    },
                    new() {
                        ProductName = "Product2",
                        EditionNumber = 2,
                        PermitExpiryDate = DateTime.UtcNow.AddDays(1).ToString("YYYY-MM-DD")
                    }
                },
                UserPermits = new List<UserPermit>
                {
                    new() {
                        Title = "UPN1",
                        Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                    },
                    new() {
                        Title = "UPN2",
                        Upn = "7B5CED73389DECDB110E6E803F957253F0DE13D1G7H8I9"
                    }
                }
            };

            var result = await _permitApiClient.GetPermitAsync(apiVersion, productType, permitRequest, correlationId);

            var isSuccess = result.IsSuccess(out var permitResponse);

            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.True);
                Assert.That(permitResponse, Is.Not.Null);
                Assert.That(permitResponse, Is.EqualTo(expectedStream));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/v1/permits/s100"));
            });
        }

        [Test]
        public async Task GetPermitAsync_Failure_ReturnsFailureResult()
        {
            var correlationId = Guid.NewGuid().ToString();
            var apiVersion = "v1";
            var productType = "s100";
            var permitRequest = new PermitRequest
            {
                Products = new List<Product>
                {
                    new() {
                        ProductName = "Product1",
                        EditionNumber = 1,
                        PermitExpiryDate = DateTime.UtcNow.AddDays(1).ToString("YYYY-MM-DD")
                    },
                    new() {
                        ProductName = "Product2",
                        EditionNumber = 2,
                        PermitExpiryDate = DateTime.UtcNow.AddDays(1).ToString("YYYY-MM-DD")
                    }
                },
                UserPermits = new List<UserPermit>
                {
                    new() {
                        Title = "UPN1",
                        Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                    },
                    new() {
                        Title = "UPN2",
                        Upn = "7B5CED73389DECDB110E6E803F957253F0DE13D1G7H8I9"
                    }
                }
            };

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(GetExpectedXmlString()));
            _responseBody.Enqueue(expectedStream);
            _responseStatusCode = HttpStatusCode.BadRequest;

            var result = await _permitApiClient.GetPermitAsync(apiVersion, productType, permitRequest, correlationId);

            var isSuccess = result.IsSuccess(out var value, out var error);
            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.False);
                Assert.That(value, Is.Null);
                Assert.That(error, Is.Not.Null);
            });
        }

        [Test]
        public async Task TestGetPermitAsyncSetsRequestHeader()
        {
            var correlationId = Guid.NewGuid().ToString();
            var apiVersion = "v1";
            var productType = "s100";
            var permitRequest = new PermitRequest
            {
                Products = new List<Product>
                {
                    new() {
                        ProductName = "Product1",
                        EditionNumber = 1,
                        PermitExpiryDate = DateTime.UtcNow.AddDays(1).ToString("YYYY-MM-DD")
                    },
                    new() {
                        ProductName = "Product2",
                        EditionNumber = 2,
                        PermitExpiryDate = DateTime.UtcNow.AddDays(1).ToString("YYYY-MM-DD")
                    }
                },
                UserPermits = new List<UserPermit>
                {
                    new() {
                        Title = "UPN1",
                        Upn = "869D4E0E902FA2E1B934A3685E5D0E85C1FDEC8BD4E5F6"
                    },
                    new() {
                        Title = "UPN2",
                        Upn = "7B5CED73389DECDB110E6E803F957253F0DE13D1G7H8I9"
                    }
                }
            };

            var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes(GetExpectedXmlString()));
            _responseBody.Enqueue(expectedStream);

            var result = await _permitApiClient.GetPermitAsync(apiVersion, productType, permitRequest, correlationId);

            Assert.That(_fakePermitHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakePermitHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("bearer"));
                Assert.That(_fakePermitHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo(DUMMY_ACCESS_TOKEN));
                Assert.That(_fakePermitHttpClientFactory.HttpClient.DefaultRequestHeaders.GetValues(XCorrelationIdHeaderKey).FirstOrDefault(), Is.EqualTo(correlationId));
            });
        }


        private static string GetExpectedXmlString()
        {
            var sb = new StringBuilder();
            sb.Append("<?xmlversion=\"1.0\"encoding=\"UTF-8\"standalone=\"yes\"?><Permitxmlns:S100SE=\"http://www.iho.int/s100/se/5.2\"xmlns:ns2=\"http://standards.iso.org/iso/19115/-3/gco/1.0\"xmlns=\"http://www.iho.int/s100/se/5.2\"><S100SE:header>");
            sb.Append("<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:dataServerName>fakeDataServerName</S100SE:dataServerName><S100SE:dataServerIdentifier>fakeDataServerIdentifier</S100SE:dataServerIdentifier><S100SE:version>1</S100SE:version>");
            sb.Append("<S100SE:userpermit>fakeUserPermit</S100SE:userpermit></S100SE:header><S100SE:products><S100SE:productid=\"fakeID\"><S100SE:datasetPermit><S100SE:filename>fakefilename</S100SE:filename><S100SE:editionNumber>1</S100SE:editionNumber>");
            sb.Append("<S100SE:issueDate>2024-09-02+01:00</S100SE:issueDate><S100SE:expiry>2024-09-02</S100SE:expiry><S100SE:encryptedKey>fakeencryptedkey</S100SE:encryptedKey></S100SE:datasetPermit></S100SE:product></S100SE:products></Permit>");

            return sb.ToString();
        }
    }
}
