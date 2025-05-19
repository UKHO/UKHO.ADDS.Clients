using System.Net;
using FakeItEasy;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests.Helpers;
using FakeFssHttpClientFactory = UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests.Helpers.FakeFssHttpClientFactory;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests
{
    internal class AddFilesToBatchTests
    {
        private object _nextResponse;
        private FileShareReadWriteClient _fileShareReadWriteClient;
        private HttpStatusCode _nextResponseStatusCode;
        private List<(HttpMethod HttpMethod, Uri Uri)> _lastRequestUris;
        private List<string> _lastRequestBodies;
        private const int MaxBlockSize = 32;
        private FakeFssHttpClientFactory _fakeFssHttpClientFactory;
        private const string DummyAccessToken = "ACarefullyEncodedSecretAccessToken";
        private const string FileName1 = "File1.bin";
        private const string FileName2 = "File2.bin";
        private const string MimeType1 = "application/octet-stream";
        private const string MimeType2 = "application/octet-stream";

        [SetUp]
        public void Setup()
        {
            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(request =>
            {
                _lastRequestUris.Add((request.Method, request.RequestUri));

                if (request.Content is StringContent content && request.Content.Headers.ContentLength.HasValue)
                {
                    _lastRequestBodies.Add(content.ReadAsStringAsync().Result);
                }
                else
                {
                    _lastRequestBodies.Add(null);
                }

                return (_nextResponseStatusCode, _nextResponse);
            });

            _nextResponse = new object();
            _nextResponseStatusCode = HttpStatusCode.Created;
            _lastRequestUris = new List<(HttpMethod HttpMethod, Uri Uri)>();
            _lastRequestBodies = new List<string>();
            _fileShareReadWriteClient = new FileShareReadWriteClient(_fakeFssHttpClientFactory, @"https://fss-tests.net", DummyAccessToken, MaxBlockSize);
        }

        [Test]
        public async Task WhenUnseekableStream_ThenThrowsException()
        {
            var expectedBatchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = expectedBatchId };
            var batchHandleResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel { BusinessUnit = "TestUnit" });
            Assert.That(batchHandleResult.IsSuccess(out var batchHandle), Is.True);
            Assert.That(batchHandle?.BatchId, Is.EqualTo(expectedBatchId));

            var stream1 = A.Fake<Stream>();
            A.CallTo(() => stream1.CanSeek).Returns(false);
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, correlationId, CancellationToken.None);
                Assert.Fail("Expected an exception");
            }
            catch (ArgumentException ex)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(ex.ParamName, Is.EqualTo("stream"));
#if NET48
                   Assert.That(ex.Message, Is.EqualTo("The stream must be seekable.\r\nParameter name: stream"));  
#elif NET8_0
                    Assert.That(ex.Message, Is.EqualTo("The stream must be seekable. (Parameter 'stream')"));
#else
                   Assert.Fail("Framework not catered for.");  
#endif
                });
            }
        }

        [Test]
        public async Task WhenUnseekableStream_ThenThrowsExceptionWithCancellationToken()
        {
            var expectedBatchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = expectedBatchId };
            var batchHandleResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel { BusinessUnit = "TestUnit" }, CancellationToken.None);
            Assert.That(batchHandleResult.IsSuccess(out var batchHandle), Is.True);
            Assert.That(batchHandle?.BatchId, Is.EqualTo(expectedBatchId));

            var stream1 = A.Fake<Stream>();
            A.CallTo(() => stream1.CanSeek).Returns(false);
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, correlationId, CancellationToken.None);
                Assert.Fail("Expected an exception");
            }
            catch (ArgumentException ex)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(ex.ParamName, Is.EqualTo("stream"));
#if NET48
                            Assert.That(ex.Message, Is.EqualTo("The stream must be seekable.\r\nParameter name: stream"));
#elif NET8_0
                    Assert.That(ex.Message, Is.EqualTo("The stream must be seekable. (Parameter 'stream')"));
#else
                            Assert.Fail("Framework not catered for.");                    
#endif
                });
            }
        }

        [Test]
        public async Task TestAddSmallFilesToBatch()
        {
            var expectedBatchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = expectedBatchId };
            var batchHandleResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel { BusinessUnit = "TestUnit" });
            Assert.That(batchHandleResult.IsSuccess(out var batchHandle), Is.True);
            Assert.That(batchHandle?.BatchId, Is.EqualTo(expectedBatchId));

            Stream stream1 = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            Stream stream2 = new MemoryStream(new byte[] { 2, 3, 4, 5, 6, 7, 8 });
            var correlationId = Guid.NewGuid().ToString();

            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, correlationId);
            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream2, FileName2, MimeType2, correlationId);

            var expectedRequests = new[]
            {
                        "POST:/batch",
                        $"POST:/batch/{expectedBatchId}/files/{FileName1}",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName1}/00001",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName1}",
                        $"POST:/batch/{expectedBatchId}/files/{FileName2}",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName2}/00001",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName2}"
                    };
            var actualRequests = _lastRequestUris.Select(x => $"{x.HttpMethod}:{x.Uri?.AbsolutePath}");
            Assert.Multiple(() =>
            {
                Assert.That(actualRequests, Is.EqualTo(expectedRequests));
                Assert.That(stream1.CanSeek, Is.True);
                Assert.That(stream2.CanSeek, Is.True);
            });
        }

        [Test]
        public async Task TestAddSmallFilesToBatchWithCancellationToken()
        {
            var expectedBatchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = expectedBatchId };
            var batchHandleResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel { BusinessUnit = "TestUnit" }, CancellationToken.None);
            Assert.That(batchHandleResult.IsSuccess(out var batchHandle), Is.True);
            Assert.That(batchHandle?.BatchId, Is.EqualTo(expectedBatchId)); // Fixed: Removed 'Data' property access

            Stream stream1 = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            Stream stream2 = new MemoryStream(new byte[] { 2, 3, 4, 5, 6, 7, 8 });
            var correlationId = Guid.NewGuid().ToString();

            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, correlationId, CancellationToken.None); // Fixed: Removed 'Data' property access
            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream2, FileName2, MimeType2, correlationId, CancellationToken.None); // Fixed: Removed 'Data' property access

            var expectedRequests = new[]
            {
                "POST:/batch",
                $"POST:/batch/{expectedBatchId}/files/{FileName1}",
                $"PUT:/batch/{expectedBatchId}/files/{FileName1}/00001",
                $"PUT:/batch/{expectedBatchId}/files/{FileName1}",
                $"POST:/batch/{expectedBatchId}/files/{FileName2}",
                $"PUT:/batch/{expectedBatchId}/files/{FileName2}/00001",
                $"PUT:/batch/{expectedBatchId}/files/{FileName2}"
            };
            var actualRequests = _lastRequestUris.Select(x => $"{x.HttpMethod}:{x.Uri?.AbsolutePath}");
            Assert.Multiple(() =>
            {
                Assert.That(actualRequests, Is.EqualTo(expectedRequests));
                Assert.That(stream1.CanSeek, Is.True);
                Assert.That(stream2.CanSeek, Is.True);
            });
        }

        [Test]
        public async Task TestAddSmallFilesToBatchWithFileAttributes()
        {
            var expectedBatchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = expectedBatchId };
            var batchHandleResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel { BusinessUnit = "TestUnit" });
            Assert.That(batchHandleResult.IsSuccess(out var batchHandle), Is.True);
            Assert.That(batchHandle?.BatchId, Is.EqualTo(expectedBatchId));

            Stream stream1 = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            Stream stream2 = new MemoryStream(new byte[] { 2, 3, 4, 5, 6, 7, 8 });
            var correlationId = Guid.NewGuid().ToString();

            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, correlationId, new KeyValuePair<string, string>("fileAttributeKey1", "fileAttributeValue1"));
            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream2, FileName2, MimeType2, correlationId, new KeyValuePair<string, string>("fileAttributeKey2", "fileAttributeValue2"));

            var expectedRequests = new[]
            {
                "POST:/batch",
                $"POST:/batch/{expectedBatchId}/files/{FileName1}",
                $"PUT:/batch/{expectedBatchId}/files/{FileName1}/00001",
                $"PUT:/batch/{expectedBatchId}/files/{FileName1}",
                $"POST:/batch/{expectedBatchId}/files/{FileName2}",
                $"PUT:/batch/{expectedBatchId}/files/{FileName2}/00001",
                $"PUT:/batch/{expectedBatchId}/files/{FileName2}"
            };
            var actualRequests = _lastRequestUris.Select(x => $"{x.HttpMethod}:{x.Uri?.AbsolutePath}");
            var addFile1Request = _lastRequestBodies[1];
            var addFile2Request = _lastRequestBodies[4];

            Assert.Multiple(() =>
            {
                Assert.That(actualRequests, Is.EqualTo(expectedRequests));
                Assert.That(addFile1Request.Replace("\r", "").Replace("\n", "").Replace(" ", ""),
                    Is.EqualTo("{\"attributes\":[{\"key\":\"fileAttributeKey1\",\"value\":\"fileAttributeValue1\"}]}"));
                Assert.That(addFile2Request.Replace("\r", "").Replace("\n", "").Replace(" ", ""),
                    Is.EqualTo("{\"attributes\":[{\"key\":\"fileAttributeKey2\",\"value\":\"fileAttributeValue2\"}]}"));
                Assert.That(stream1.CanSeek, Is.True);
                Assert.That(stream2.CanSeek, Is.True);
            });
        }

        [Test]
        public async Task TestAddSmallFilesToBatchWithFileAttributesWithCancellationToken()
        {
            var expectedBatchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = expectedBatchId };
            var batchHandleResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel { BusinessUnit = "TestUnit" }, CancellationToken.None);
            Assert.That(batchHandleResult.IsSuccess(out var batchHandle), Is.True);
            Assert.That(batchHandle?.BatchId, Is.EqualTo(expectedBatchId));

            Stream stream1 = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            Stream stream2 = new MemoryStream(new byte[] { 2, 3, 4, 5, 6, 7, 8 });
            var correlationId = Guid.NewGuid().ToString();

            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, correlationId, CancellationToken.None, new KeyValuePair<string, string>("fileAttributeKey1", "fileAttributeValue1"));
            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream2, FileName2, MimeType2, correlationId, CancellationToken.None, new KeyValuePair<string, string>("fileAttributeKey2", "fileAttributeValue2"));

            var expectedRequests = new[]
            {
                        "POST:/batch",
                        $"POST:/batch/{expectedBatchId}/files/{FileName1}",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName1}/00001",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName1}",
                        $"POST:/batch/{expectedBatchId}/files/{FileName2}",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName2}/00001",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName2}"
                    };
            var actualRequests = _lastRequestUris.Select(x => $"{x.HttpMethod}:{x.Uri?.AbsolutePath}");
            var addFile1Request = _lastRequestBodies[1];
            var addFile2Request = _lastRequestBodies[4];
            Assert.Multiple(() =>
            {
                Assert.That(actualRequests, Is.EqualTo(expectedRequests));
                Assert.That(addFile1Request.Replace("\r", "").Replace("\n", "").Replace(" ", ""),
                    Is.EqualTo("{\"attributes\":[{\"key\":\"fileAttributeKey1\",\"value\":\"fileAttributeValue1\"}]}"));
                Assert.That(addFile2Request.Replace("\r", "").Replace("\n", "").Replace(" ", ""),
                    Is.EqualTo("{\"attributes\":[{\"key\":\"fileAttributeKey2\",\"value\":\"fileAttributeValue2\"}]}"));
                Assert.That(stream1.CanSeek, Is.True);
                Assert.That(stream2.CanSeek, Is.True);
            });
        }

        [Test]
        public async Task TestAddLargerFileToBatch()
        {
            var expectedBatchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = expectedBatchId };
            var batchHandleResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel { BusinessUnit = "TestUnit" });
            Assert.That(batchHandleResult.IsSuccess(out var batchHandle), Is.True);

            Stream stream1 = new MemoryStream(new byte[MaxBlockSize * 3]);
            var correlationId = Guid.NewGuid().ToString();

            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, correlationId);

            var expectedRequests = new[]
            {
                "POST:/batch",
                $"POST:/batch/{expectedBatchId}/files/{FileName1}",
                $"PUT:/batch/{expectedBatchId}/files/{FileName1}/00001",
                $"PUT:/batch/{expectedBatchId}/files/{FileName1}/00002",
                $"PUT:/batch/{expectedBatchId}/files/{FileName1}/00003",
                $"PUT:/batch/{expectedBatchId}/files/{FileName1}"
            };
            var actualRequests = _lastRequestUris.Select(x => $"{x.HttpMethod}:{x.Uri?.AbsolutePath}");
            var writeBlockFileModel = _lastRequestBodies.Last()?.DeserialiseJson<WriteBlockFileModel>();
            var expectedBlockIds = new[] { "00001", "00002", "00003" };
            Assert.Multiple(() =>
            {
                Assert.That(actualRequests, Is.EqualTo(expectedRequests));
                Assert.That(writeBlockFileModel?.BlockIds, Is.EqualTo(expectedBlockIds));
                Assert.That(stream1.CanSeek, Is.True);
            });
        }

        [Test]
        public async Task TestAddLargerFileToBatchWithCancellationToken()
        {
            var expectedBatchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = expectedBatchId };
            var batchHandleResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel { BusinessUnit = "TestUnit" }, CancellationToken.None);
            Assert.That(batchHandleResult.IsSuccess(out var batchHandle), Is.True);
            Assert.That(batchHandle?.BatchId, Is.EqualTo(expectedBatchId));

            Stream stream1 = new MemoryStream(new byte[MaxBlockSize * 3]);
            var correlationId = Guid.NewGuid().ToString();

            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, correlationId, CancellationToken.None);

            var expectedRequests = new[]
            {
                        "POST:/batch",
                        $"POST:/batch/{expectedBatchId}/files/{FileName1}",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName1}/00001",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName1}/00002",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName1}/00003",
                        $"PUT:/batch/{expectedBatchId}/files/{FileName1}"
                    };
            var actualRequests = _lastRequestUris.Select(x => $"{x.HttpMethod}:{x.Uri?.AbsolutePath}");
            var writeBlockFileModel = _lastRequestBodies.Last()?.DeserialiseJson<WriteBlockFileModel>();
            var expectedBlockIds = new[] { "00001", "00002", "00003" };
            Assert.Multiple(() =>
            {
                Assert.That(actualRequests, Is.EqualTo(expectedRequests));
                Assert.That(writeBlockFileModel?.BlockIds, Is.EqualTo(expectedBlockIds));
                Assert.That(stream1.CanSeek, Is.True);
            });
        }

        [Test]
        public async Task TestProgressFeedbackWithAddLargerFileToBatch()
        {
            var expectedBatchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = expectedBatchId };
            var batchHandleResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel { BusinessUnit = "TestUnit" });
            Assert.That(batchHandleResult.IsSuccess(out var batchHandle), Is.True);
            Assert.That(batchHandle?.BatchId, Is.EqualTo(expectedBatchId));

            var stream1 = new MemoryStream(new byte[MaxBlockSize * 3 - 1]);
            var progressReports = new List<(int blocksComplete, int totalBlockCount)>();
            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, progressReports.Add);

            var expectedBlocksComplete = new[] { 0, 1, 2, 3 };
            var expectedTotalBlockCount = new[] { 3, 3, 3, 3 };
            Assert.Multiple(() =>
            {
                Assert.That(progressReports, Has.Count.EqualTo(4));
                Assert.That(progressReports.Select(r => r.blocksComplete), Is.EqualTo(expectedBlocksComplete));
                Assert.That(progressReports.Select(r => r.totalBlockCount), Is.EqualTo(expectedTotalBlockCount));
                Assert.That(stream1.CanSeek, Is.True);
            });
        }

        [Test]
        public async Task TestProgressFeedbackWithAddLargerFileToBatchWithCancellationToken()
        {
            var expectedBatchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = expectedBatchId };
            var batchHandleResult = await _fileShareReadWriteClient.CreateBatchAsync(new BatchModel { BusinessUnit = "TestUnit" }, CancellationToken.None);
            Assert.That(batchHandleResult.IsSuccess(out var batchHandle), Is.True);
            Assert.That(batchHandle?.BatchId, Is.EqualTo(expectedBatchId));

            var stream1 = new MemoryStream(new byte[MaxBlockSize * 3 - 1]);
            var correlationId = Guid.NewGuid().ToString();

            var progressReports = new List<(int blocksComplete, int totalBlockCount)>();
            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, progressReports.Add, correlationId, CancellationToken.None);

            var expectedBlocksComplete = new[] { 0, 1, 2, 3 };
            var expectedTotalBlockCount = new[] { 3, 3, 3, 3 };
            Assert.Multiple(() =>
            {
                Assert.That(progressReports, Has.Count.EqualTo(4));
                Assert.That(progressReports.Select(r => r.blocksComplete), Is.EqualTo(expectedBlocksComplete));
                Assert.That(progressReports.Select(r => r.totalBlockCount), Is.EqualTo(expectedTotalBlockCount));
                Assert.That(stream1.CanSeek, Is.True);
            });
        }

        [Test]
        public async Task TestAddFileToBatchSetsAuthorizationHeader()
        {
            var batchId = Guid.NewGuid().ToString();
            _nextResponse = new CreateBatchResponseModel { BatchId = batchId };
            var batchHandle = new BatchHandle(batchId);

            Stream stream1 = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            var correlationId = Guid.NewGuid().ToString();

            await _fileShareReadWriteClient.AddFileToBatchAsync(batchHandle, stream1, FileName1, MimeType1, correlationId, CancellationToken.None);

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo(DummyAccessToken));
                Assert.That(stream1.CanSeek, Is.True);
            });
        }

        [TearDown]
        public void TearDown()
        {
            _fakeFssHttpClientFactory.Dispose();
        }
    }
}
