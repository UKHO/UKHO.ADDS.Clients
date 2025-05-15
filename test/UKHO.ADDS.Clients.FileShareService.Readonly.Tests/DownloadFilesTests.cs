using System.Collections.Concurrent;
using System.Net;
using System.Text;
using FakeItEasy;
using NUnit.Framework;
using UKHO.ADDS.Clients.Common.Authentication;
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
        private IAuthenticationTokenProvider fakeAuthProvider;
        private string _batchId;
        private byte[] _expectedBytes;
        private MemoryStream _destStream;
        private string _fileName = "TestFile.txt";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _batchId = Guid.NewGuid().ToString();
            _expectedBytes = Encoding.UTF8.GetBytes("Contents of a file.");
            _destStream = new MemoryStream();
        }

        [SetUp]
        public void Setup()
        {
            _nextResponses = new ConcurrentQueue<object>();
            _nextResponseStatusCode = HttpStatusCode.OK;
            fakeAuthProvider = A.Fake<IAuthenticationTokenProvider>();
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

            _fileShareApiClient = new FileShareReadOnlyClient(_fakeFssHttpClientFactory,
                @"https://fss-tests.net/basePath/", DUMMY_ACCESS_TOKEN);
        }

        [TearDown]
        public void TearDown() => _fakeFssHttpClientFactory.Dispose();

        [Test]
        public async Task WhenDownloadFileAsyncIsCalledWithValidBatchIdAndFileName_ThenReturnsSuccessWithExpectedFileContents()
        {

            _nextResponses.Enqueue(new MemoryStream(_expectedBytes));

            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "AFilename.txt");

            var isSuccess = result.IsSuccess(out var batchStatusResponse);

            Assert.Multiple(() =>
            {
                Assert.That(isSuccess, Is.True);
                Assert.That(((MemoryStream)batchStatusResponse).ToArray(), Is.EqualTo(_expectedBytes));
                Assert.That(_lastRequestUri?.AbsolutePath,
                    Is.EqualTo($"/basePath/batch/{_batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task WhenDownloadFilesForABatchThatDoesNotExist_ThenReturnsFailureResult()
        {
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            try
            {
                var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "AFilename.txt");

                Assert.That(result.IsFailure);
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<HttpRequestException>());
            }

            Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{_batchId}/files/AFilename.txt"));
        }

        [Test]
        public async Task WhenDownloadFilesForABatchWithAFileThatDoesNotExist_ThenReturnsFailureResult()
        {
            _nextResponseStatusCode = HttpStatusCode.NotFound;

            try
            {
                var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "AFilename.txt");

                Assert.That(result.IsFailure);
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<HttpRequestException>());
            }

            Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{_batchId}/files/AFilename.txt"));
        }

        [Test]
        public async Task WhenGetBatchStatusForABatchThatHasBeenDeleted_ThenReturnsFailureResult()
        {
            _nextResponseStatusCode = HttpStatusCode.Gone;

            try
            {
                var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "AFile.txt");

                Assert.That(result.IsFailure);
            }
            catch (Exception e)
            {
                Assert.That(e, Is.InstanceOf<HttpRequestException>());
            }

            Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{_batchId}/files/AFile.txt"));
        }

        [Test]
        public async Task WhenDownloadFileAsyncIsCalled_ThenAuthorizationHeaderIsSet()
        {
            _nextResponses.Enqueue(new MemoryStream(_expectedBytes));

            await _fileShareApiClient.DownloadFileAsync(_batchId, "AFilename.txt");

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme,
                    Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter,
                    Is.EqualTo(DUMMY_ACCESS_TOKEN));
            });
        }

        [Test]
        public async Task WhenDownloadFileAsyncIsCalledWithCancellationToken_ThenReturnsSuccess()
        {
            _nextResponses.Enqueue(new MemoryStream(_expectedBytes));

            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "AFilename.txt", _destStream,
                _expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(_lastRequestUri?.AbsolutePath,
                    Is.EqualTo($"/basePath/batch/{_batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task WhenDownloadFileAsyncIsCalledWithCancellationToken_ThenAuthorizationHeaderIsSet()
        {
            _nextResponses.Enqueue(new MemoryStream(_expectedBytes));

            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "AFilename.txt", _destStream,
                _expectedBytes.Length, CancellationToken.None);

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme,
                    Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter,
                    Is.EqualTo(DUMMY_ACCESS_TOKEN));
            });
        }

        [Test]
        public async Task WhenDownloadFilesForABatchIsCalledThatDoesNotExistWithCancellationToken_ThenReturnsExpectedResult()
        {
            _nextResponses.Enqueue(new MemoryStream(_expectedBytes));

            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "AFilename.txt", _destStream,
                _expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath,
                    Is.EqualTo($"/basePath/batch/{_batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task WhenGetBatchStatusForABatchThatHasBeenDeletedWithCancellationToken_ThenReturnsExpectedResult()
        {
            _nextResponses.Enqueue(new MemoryStream(_expectedBytes));

            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "AFilename.txt", _destStream,
                _expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(_lastRequestUri?.AbsolutePath,
                    Is.EqualTo($"/basePath/batch/{_batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task WhenFileSizeIsGreaterThanMaxDownloadBytes_ThenDownloadsFileInMultipleParts()
        {
            _nextResponseStatusCode = HttpStatusCode.PartialContent;
            const int lengthPart1 = 10485760;
            const int lengthPart2 = 100000;
            const int totalLength = lengthPart1 + lengthPart2;
            var expectedBytes1 = new byte[lengthPart1];
            _nextResponses.Enqueue(new MemoryStream(expectedBytes1));
            var expectedBytes2 = new byte[lengthPart2];
            _nextResponses.Enqueue(new MemoryStream(expectedBytes2));
            var destStream = new MemoryStream();

            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "AFilename.txt", destStream, totalLength,
                CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(destStream.Length, Is.EqualTo(totalLength));
                Assert.That(_lastRequestUri?.AbsolutePath,
                    Is.EqualTo($"/basePath/batch/{_batchId}/files/AFilename.txt"));
            });
        }

        [Test]
        public async Task WhenFileIsDownloaded_ThenDownloadedBytesAreEqualToExpectedFileBytes()
        {
            _nextResponses.Enqueue(new MemoryStream(_expectedBytes));
            var destStream = new MemoryStream();

            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "AFilename.txt", destStream,
                _expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(destStream.Length, Is.EqualTo(_expectedBytes.Length));
            });
        }

        [Test]
        public async Task TestBasicDownloadZipFile()
        {
            var expectedBytes = new MemoryStream(Encoding.UTF8.GetBytes("Contents of a file."));
            _nextResponses.Enqueue(expectedBytes);

            var response = await _fileShareApiClient.DownloadZipFileAsync(_batchId, CancellationToken.None);

            var isSuccess = response.IsSuccess(out var responseData);

            Assert.Multiple(() =>
            {
                Assert.That(responseData, Is.EqualTo(expectedBytes));
                Assert.That(response.IsSuccess, Is.True);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{_batchId}/files"));
            });
        }

        [Test]
        public async Task TestDownloadZipFileForABatchThatDoesNotExist()
        {
            _nextResponseStatusCode = HttpStatusCode.BadRequest;

            var response = await _fileShareApiClient.DownloadZipFileAsync(_batchId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(response.IsSuccess, Is.False);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{_batchId}/files"));
            });
        }

        [Test]
        public async Task TestDownloadZipFileForABatchWithAFileThatDoesNotExist()
        {
            _nextResponseStatusCode = HttpStatusCode.NotFound;

            var response = await _fileShareApiClient.DownloadZipFileAsync(_batchId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(response.IsSuccess, Is.False);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{_batchId}/files"));
            });
        }

        [Test]
        public async Task TestGetBatchStatusForABatchZipFileThatHasBeenDeleted()
        {
            _nextResponseStatusCode = HttpStatusCode.Gone;

            var response = await _fileShareApiClient.DownloadZipFileAsync(_batchId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(response.IsSuccess, Is.False);
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{_batchId}/files"));
            });
        }

        [Test]
        public async Task TestDownloadZipFileSetsAuthorizationHeader()
        {
            _nextResponses.Enqueue(new MemoryStream(_expectedBytes));

            await _fileShareApiClient.DownloadZipFileAsync(_batchId, CancellationToken.None);

            Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Scheme,
                    Is.EqualTo("bearer"));
                Assert.That(_fakeFssHttpClientFactory.HttpClient.DefaultRequestHeaders.Authorization.Parameter,
                    Is.EqualTo(DUMMY_ACCESS_TOKEN));
            });
        }

        [Test]
        public async Task DownloadFileAsync_ReturnsSuccessWithDownloadFileResponseContainingDestinationStream()
        {
            var sourceStream = new MemoryStream(_expectedBytes);
            _nextResponses.Enqueue(sourceStream);

            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, "TestFile.txt", _destStream,
                _expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(_destStream.ToArray(), Is.EqualTo(_expectedBytes));
            });
        }

        [Test]
        public async Task WhenDownloadFileAsyncIsCalled_ThenReturnsSuccessWithDownloadFileResponseContainingDestinationStream()
        {
            var correlationId = Guid.NewGuid().ToString();
            var sourceStream = new MemoryStream(_expectedBytes);
            _nextResponses.Enqueue(sourceStream);
            var destinationStream = new MemoryStream();

            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, _fileName, destinationStream,
                correlationId, _expectedBytes.Length, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(destinationStream.ToArray(), Is.EqualTo(_expectedBytes));
                Assert.That(_lastRequestUri?.AbsolutePath, Is.EqualTo($"/basePath/batch/{_batchId}/files/{_fileName}"));
            });
        }

        [Test]
        public async Task WhenDownloadFileAsyncIsCalledAndBadRequestOccurs_ThenReturnsFailureResult()
        {
            _nextResponseStatusCode = HttpStatusCode.BadRequest;
            var client = new FileShareReadOnlyClient(_fakeFssHttpClientFactory, "https://fss-tests.net/basePath/",
                fakeAuthProvider);

            var result =
                await client.DownloadFileAsync(_batchId, _fileName, _destStream, 100, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess(out var value, out var errors), Is.False);
                Assert.That(errors?.Message, Is.Not.Null);
            });
        }

        [Test]
        public async Task WhenExceptionIsThrownDuringDownloadFileAsync_ThenReturnsFailureResult()
        {
            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(_ => throw new HttpRequestException("Simulated exception"));
            _fileShareApiClient = new FileShareReadOnlyClient(_fakeFssHttpClientFactory, "https://fss-tests.net/basePath/", fakeAuthProvider);

            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, _fileName);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess(out var value, out var errors), Is.False, "The result should indicate failure.");
                Assert.That(errors?.Message, Is.EqualTo("Simulated exception"), "The error message should match the simulated exception.");
            });
        }

        [Test]

        public async Task WhenExceptionIsThrownDuringDownloadFileAsync_ThenReturnsFailureResultWithExceptionMessage()
        {
            const string exceptionMessage = "Test exception";

            _fakeFssHttpClientFactory = new FakeFssHttpClientFactory(_ => throw new Exception(exceptionMessage));
            _fileShareApiClient = new FileShareReadOnlyClient(_fakeFssHttpClientFactory, @"https://fss-tests.net/basePath/", DUMMY_ACCESS_TOKEN);
            var result = await _fileShareApiClient.DownloadFileAsync(_batchId, _fileName, _destStream, 100, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo(exceptionMessage));
            });
        }
    }
}
