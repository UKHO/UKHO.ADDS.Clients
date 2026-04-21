using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using UKHO.ADDS.Clients.Common.MiddlewareExtensions;

namespace UKHO.ADDS.Clients.Common.Tests.MiddlewareExtensions;

[TestFixture]
public class KiotaServiceCollectionExtensionsTests
{
    [Test]
    public void AddKiotaDefaults_WhenCalled_RegistersAuthenticationProviderAndClientFactory()
    {
        var services = new ServiceCollection();
        var authProvider = A.Fake<IAuthenticationProvider>();

        services.AddLogging();
        services.AddHttpClient();
        services.AddKiotaDefaults(authProvider);

        using var serviceProvider = services.BuildServiceProvider();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(serviceProvider.GetRequiredService<IAuthenticationProvider>(), Is.SameAs(authProvider));
            Assert.That(serviceProvider.GetRequiredService<ClientFactory>(), Is.Not.Null);
        }
    }

    [Test]
    public void ClientFactory_WhenClientHasRequestAdapterConstructor_ReturnsClient()
    {
        var authProvider = A.Fake<IAuthenticationProvider>();
        var httpClientFactory = A.Fake<IHttpClientFactory>();
        var logger = A.Fake<ILogger<ClientFactory>>();
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://example.test/")
        };

        A.CallTo(() => httpClientFactory.CreateClient(nameof(TestKiotaClient))).Returns(httpClient);

        var sut = new ClientFactory(authProvider, httpClientFactory, logger);

        var client = sut.GetClient<TestKiotaClient>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(client, Is.Not.Null);
            Assert.That(client.RequestAdapter, Is.Not.Null);
        }

        A.CallTo(() => httpClientFactory.CreateClient(nameof(TestKiotaClient))).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void ClientFactory_WhenClientDoesNotExposeRequestAdapterConstructor_ThrowsInvalidOperationException()
    {
        var authProvider = A.Fake<IAuthenticationProvider>();
        var httpClientFactory = A.Fake<IHttpClientFactory>();
        var logger = A.Fake<ILogger<ClientFactory>>();
        var sut = new ClientFactory(authProvider, httpClientFactory, logger);

        var exception = Assert.Throws<InvalidOperationException>(() => sut.GetClient<InvalidKiotaClient>());

        Assert.That(exception!.Message, Is.EqualTo("InvalidKiotaClient must have a constructor with IRequestAdapter parameter."));
    }

    [Test]
    public void RegisterKiotaClient_WithEndpointConfigKey_RegistersSingletonClientAndConfiguredHttpClient()
    {
        const string endpointConfigKey = "Services:SalesCatalogue";
        var headers = new Dictionary<string, string>
        {
            ["X-Test-Header"] = "header-value"
        };

        var configuration = A.Fake<IConfiguration>();
        A.CallTo(() => configuration[endpointConfigKey]).Returns("https://example.test/api/");

        var services = new ServiceCollection();
        var authProvider = A.Fake<IAuthenticationProvider>();

        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddKiotaDefaults(authProvider);
        services.RegisterKiotaClient<TestKiotaClient>(endpointConfigKey, headers);

        using var serviceProvider = services.BuildServiceProvider();

        var firstClient = serviceProvider.GetRequiredService<TestKiotaClient>();
        var secondClient = serviceProvider.GetRequiredService<TestKiotaClient>();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(TestKiotaClient));
        var headersOption = serviceProvider.GetRequiredService<HeadersInspectionHandlerOption>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstClient, Is.SameAs(secondClient));
            Assert.That(httpClient.BaseAddress, Is.EqualTo(new Uri("https://example.test/api/")));
            Assert.That(httpClient.DefaultRequestHeaders.GetValues("X-Test-Header").Single(), Is.EqualTo("header-value"));
            Assert.That(headersOption.InspectResponseHeaders, Is.True);
        }
    }

    [Test]
    public void RegisterKiotaClient_WithEndpointAndAuthenticationFactory_RegistersTransientClientWithTrimmedBaseUrl()
    {
        var services = new ServiceCollection();
        var authProvider = A.Fake<IAuthenticationProvider>();

        services.AddLogging();
        services.AddSingleton<TestDependency>();
        services.RegisterKiotaClient<TestKiotaClientWithDependency>(
            _ => (new Uri("https://example.test/root/"), authProvider),
            new Dictionary<string, string> { ["X-Test-Header"] = "header-value" });

        using var serviceProvider = services.BuildServiceProvider();

        var firstClient = serviceProvider.GetRequiredService<TestKiotaClientWithDependency>();
        var secondClient = serviceProvider.GetRequiredService<TestKiotaClientWithDependency>();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(nameof(TestKiotaClientWithDependency));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstClient, Is.Not.SameAs(secondClient));
            Assert.That(firstClient.Dependency, Is.Not.Null);
            Assert.That(firstClient.RequestAdapter.BaseUrl, Is.EqualTo("https://example.test/root"));
            Assert.That(httpClient.BaseAddress, Is.EqualTo(new Uri("https://example.test/root/")));
            Assert.That(httpClient.DefaultRequestHeaders.GetValues("X-Test-Header").Single(), Is.EqualTo("header-value"));
        }
    }

    private sealed class TestKiotaClient(IRequestAdapter requestAdapter)
    {
        public IRequestAdapter RequestAdapter { get; } = requestAdapter;
    }

    private sealed class TestKiotaClientWithDependency(IRequestAdapter requestAdapter, KiotaServiceCollectionExtensionsTests.TestDependency dependency)
    {
        public IRequestAdapter RequestAdapter { get; } = requestAdapter;

        public TestDependency Dependency { get; } = dependency;
    }

    private sealed class InvalidKiotaClient
    {
    }

    private sealed class TestDependency
    {
    }
}
