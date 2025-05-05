using FakeItEasy;
using NUnit.Framework;
using UKHO.ADDS.Clients.Common.Authentication;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests
{
    [TestFixture]
    public class FileShareReadWriteClientFactoryTests
    {
        private const string DummyAccessToken = "ACarefullyEncodedSecretAccessToken";
        private const string DummyBaseAddress = "https://test.com";
        private IHttpClientFactory _fakeHttpClientFactory;
        private FileShareReadWriteClientFactory _fileShareReadWriteClientFactory;

        [SetUp]
        public void Setup()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
            _fileShareReadWriteClientFactory = new FileShareReadWriteClientFactory(_fakeHttpClientFactory);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new FileShareReadWriteClientFactory(null));
            Assert.Multiple(() =>
            {
                Assert.That(exception, Is.Not.Null);
                Assert.That(exception.ParamName, Is.EqualTo("clientFactory"));
            });
        }

        [Test]
        public void WhenConstructorIsCalled_ThenHttpClientFactoryIsInitialized()
        {
            var factory = new FileShareReadWriteClientFactory(_fakeHttpClientFactory);

            Assert.Multiple(() =>
            {
                Assert.That(factory, Is.Not.Null);
                Assert.That(factory, Is.InstanceOf<FileShareReadWriteClientFactory>());
            });
        }

        [Test]
        public void WhenCreateClientIsCalledWithAccessToken_ThenReturnsFileShareReadWriteClient()
        {
            var client = _fileShareReadWriteClientFactory.CreateClient(DummyBaseAddress, DummyAccessToken);

            Assert.Multiple(() =>
            {
                Assert.That(client, Is.Not.Null);
                Assert.That(client, Is.InstanceOf<IFileShareReadWriteClient>());
            });
        }

        [Test]
        public void WhenCreateClientIsCalledWithTokenProvider_ThenReturnsFileShareReadWriteClient()
        {
            var fakeTokenProvider = A.Fake<IAuthenticationTokenProvider>();

            var client = _fileShareReadWriteClientFactory.CreateClient(DummyBaseAddress, fakeTokenProvider);

            Assert.Multiple(() =>
            {
                Assert.That(client, Is.Not.Null);
                Assert.That(client, Is.InstanceOf<IFileShareReadWriteClient>());
            });
        }        

        [Test]
        public void WhenCreateClientIsCalledWithEmptyBaseAddress_ThenThrowsArgumentException()
        {
            Assert.Throws<UriFormatException>(() =>
                _fileShareReadWriteClientFactory.CreateClient(string.Empty, DummyAccessToken));
        }
    }
}
