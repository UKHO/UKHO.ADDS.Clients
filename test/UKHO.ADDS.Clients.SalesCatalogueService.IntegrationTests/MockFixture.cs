using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using UKHO.ADDS.Clients.AppHost.Constants;

namespace UKHO.ADDS.Clients.SalesCatalogueService.IntegrationTests
{
    internal sealed class SampleServiceFixture
    {
        public Uri BaseAddress { get; private set; } = null!;

        private DistributedApplication _app = null!;

        public async Task StartAsync()
        {
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.UKHO_ADDS_Clients_AppHost>();
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });
            _app = await appHost.BuildAsync();

            var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();
            await _app.StartAsync();
            await resourceNotificationService.WaitForResourceAsync(ProcessNames.MockService, KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
            BaseAddress = _app.GetEndpoint(ProcessNames.MockService);
        }

        public async Task StopAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
        }
    }
}
