using UKHO.ADDS.Mocks.Configuration;
using UKHO.ADDS.Mocks.Domain.Configuration;

namespace UKHO.ADDS.Mocks.Clients
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            MockServices.AddServices();
            ServiceRegistry.AddDefinition(new ServiceDefinition("scs", "Sales Catalogue Service", []));
            await MockServer.RunAsync(args);
        }
    }
}
