using System.Collections.Concurrent;
using System.Net;
using System.Text;
using NUnit.Framework;
using UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests.Helpers;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests
{
    public class DownloadFilesTests
    {
        private const string DUMMY_ACCESS_TOKEN = "ACarefullyEncodedSecretAccessToken";
        private FakeFssHttpClientFactory _fakeFssHttpClientFactory;
        private FileShareReadOnlyClient _fileShareApiClient;
        private Uri _lastRequestUri;
        private ConcurrentQueue<object> _nextResponses;
        private HttpStatusCode _nextResponseStatusCode;

        [SetUp]
        public void Setup()
        {
            _nextResponses = new ConcurrentQueue<object>();
            _nextResponseStatusCode = HttpStatusCode.OK;
            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(request =>
            {
                _lastRequestUri = request.RequestUri;

                if (_nextResponses.IsEmpty)
                {
                    return (_nextResponseStatusCode, new object());
                }

                if (_nextResponses.TryDequeue(out var response))
                {
                    return (_nextResponseStatusCode, response);
                }

                throw new Exception("Failed to dequeue next response");
            });

            _fileShareApiClient = new FileShareReadOnlyClient(_fakeFssHttpClientFactory, @"https://fss-tests.net/basePath/", DUMMY_ACCESS_TOKEN);
        }

        [TearDown]
        public void TearDown() => _fakeFssHttpClientFactory.Dispose();

        [Test]
        public async Task TestBasicDownloadFile()
        {
            var batchId = Guid.NewGuid().ToString();
            var expectedBytes = Encoding.UTF8.GetBytes("Contents of a file.");
            _nextResponses.Enqueue(new MemoryStream(expectedBytes));

            var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt");

            var isSuccess = result.IsSuccess(out var batchStatusResponse);

            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.True);
                Assert.That(((MemoryStream)batchStatusResponse).ToArray(), Is.EqualTo(expectedBytes));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task TestDownloadFilesForABatchThatDoesNotExist()
        {
            var batchId = Guid.NewGuid().ToString();
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            try
            {
                var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt");

                Assert.That(result.IsFailure);
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<HttpRequestException>());
            }

            Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files/AFilename.txt"));
        }

        [Test]
        public async Task TestDownloadFilesForABatchWithAFileThatDoesNotExist()
        {
            var batchId = Guid.NewGuid().ToString();
            _nextResponseStatusCode = HttpStatusCode.NotFound;

            try
            {
                var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt");

                Assert.That(result.IsFailure);
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<HttpRequestException>());
            }

            Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files/AFilename.txt"));
        }

        [Test]
        public async Task TestGetBatchStatusForABatchThatHasBeenDeleted()
        {
            var batchId = Guid.NewGuid().ToString();
            _nextResponseStatusCode = HttpStatusCode.Gone;

            try
            {
                var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFile.txt");

                Assert.That(result.IsFailure);
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<HttpRequestException>());
            }

            Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files/AFile.txt"));
        }

        [Test]
        public async Task TestDownloadFileSetsAuthorizationHeader()
        {
            var batchId = Guid.NewGuid().ToString();
            var expectedBytes = Encoding.UTF8.GetBytes("Contents of a file.");
            _nextResponses.Enqueue(new MemoryStream(expectedBytes));

            await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt");

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo(DUMMY_ACCESS_TOKEN));
            });
        }

        [Test]
        public async Task TestBasicDownloadFileWithCancellationToken()
        {
            var batchId = Guid.NewGuid().ToString();
            var expectedBytes = Encoding.UTF8.GetBytes("Contents of a file.");
            _nextResponses.Enqueue(new MemoryStream(expectedBytes));
            var destStream = new MemoryStream();

            var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt", destStream, expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task TestDownloadFileSetsAuthorizationHeaderWithCancellationToken()
        {
            var batchId = Guid.NewGuid().ToString();
            var expectedBytes = Encoding.UTF8.GetBytes("Contents of a file.");
            _nextResponses.Enqueue(new MemoryStream(expectedBytes));
            var destStream = new MemoryStream();

            var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt", destStream, expectedBytes.Length, CancellationToken.None);

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo(DUMMY_ACCESS_TOKEN));
            });
        }

        [Test]
        public async Task TestDownloadFilesForABatchThatDoesNotExistWithCancellationToken()
        {
            var batchId = Guid.NewGuid().ToString();
            var expectedBytes = Encoding.UTF8.GetBytes("Contents of a file.");
            _nextResponses.Enqueue(new MemoryStream(expectedBytes));
            var destStream = new MemoryStream();

            var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt", destStream, expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                //Assert.That(result.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task TestDownloadFilesForABatchWithAFileThatDoesNotExistWithCancellationToken()
        {
            var batchId = Guid.NewGuid().ToString();
            var expectedBytes = Encoding.UTF8.GetBytes("Contents of a file.");
            _nextResponses.Enqueue(new MemoryStream(expectedBytes));
            var destStream = new MemoryStream();

            var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt", destStream, expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                //Assert.That(result.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task TestGetBatchStatusForABatchThatHasBeenDeletedWithCancellationToken()
        {
            var batchId = Guid.NewGuid().ToString();
            var expectedBytes = Encoding.UTF8.GetBytes("Contents of a file.");
            _nextResponses.Enqueue(new MemoryStream(expectedBytes));
            var destStream = new MemoryStream();

            var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt", destStream, expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                //Assert.That(result.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task TestForDownloadFilesWhenFileSizeIsGreaterThanMaxDownloadBytes()
        {
            var batchId = Guid.NewGuid().ToString();
            _nextResponseStatusCode = HttpStatusCode.PartialContent;
            const int lengthPart1 = 10485760;
            const int lengthPart2 = 100000;
            const int totalLength = lengthPart1 + lengthPart2;
            var expectedBytes1 = new byte[lengthPart1];
            _nextResponses.Enqueue(new MemoryStream(expectedBytes1));
            var expectedBytes2 = new byte[lengthPart2];
            _nextResponses.Enqueue(new MemoryStream(expectedBytes2));
            var destStream = new MemoryStream();

            var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt", destStream, totalLength, CancellationToken.None);

            Assert.Multiple(() =>
            {
                //Assert.That(result.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(destStream.Length, Is.EqualTo(totalLength));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task TestForDownloadedFilesbytesIsEqualToExpectedFileBytes()
        {
            var batchId = Guid.NewGuid().ToString();
            var expectedBytes = Encoding.UTF8.GetBytes("Contents of a file.");
            _nextResponses.Enqueue(new MemoryStream(expectedBytes));
            var destStream = new MemoryStream();

            var result = await _fileShareApiClient.DownloadFileAsync(batchId, "AFilename.txt", destStream, expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                //Assert.That(result.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(destStream.Length, Is.EqualTo(expectedBytes.Length));
            });
        }

        [Test]
        public async Task TestBasicDownloadZipFile()
        {
            var batchId = Guid.NewGuid().ToString();

            var expectedBytes = new MemoryStream(Encoding.UTF8.GetBytes("Contents of a file."));
            _nextResponses.Enqueue(expectedBytes);

            var response = await _fileShareApiClient.DownloadZipFileAsync(batchId, CancellationToken.None);

            var isSuccess = response.IsSuccess(out var responseData);

            Assert.Multiple(() =>
            {
                Assert.That(responseData, Is.EqualTo(expectedBytes));
                Assert.That(response.IsSuccess, Is.True);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files"));
            });
        }

        [Test]
        public async Task TestDownloadZipFileForABatchThatDoesNotExist()
        {
            var batchId = Guid.NewGuid().ToString();
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            var response = await _fileShareApiClient.DownloadZipFileAsync(batchId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                //Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.False);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files"));
            });
        }

        [Test]
        public async Task TestDownloadZipFileForABatchWithAFileThatDoesNotExist()
        {
            var batchId = Guid.NewGuid().ToString();
            _nextResponseStatusCode = HttpStatusCode.NotFound;

            var response = await _fileShareApiClient.DownloadZipFileAsync(batchId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                //Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.False);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files"));
            });
        }

        [Test]
        public async Task TestGetBatchStatusForABatchZipFileThatHasBeenDeleted()
        {
            var batchId = Guid.NewGuid().ToString();
            _nextResponseStatusCode = HttpStatusCode.Gone;

            var response = await _fileShareApiClient.DownloadZipFileAsync(batchId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                //Assert.That(response.StatusCode, Is.EqualTo((int)_nextResponseStatusCode));
                Assert.That(response.IsSuccess, Is.False);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{batchId}/files"));
            });
        }

        [Test]
        public async Task TestDownloadZipFileSetsAuthorizationHeader()
        {
            var batchId = Guid.NewGuid().ToString();
            var expectedBytes = Encoding.UTF8.GetBytes("Contents of a file.");
            _nextResponses.Enqueue(new MemoryStream(expectedBytes));

            await _fileShareApiClient.DownloadZipFileAsync(batchId, CancellationToken.None);

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme, Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter, Is.EqualTo(DUMMY_ACCESS_TOKEN));
            });
        }
    }
}
