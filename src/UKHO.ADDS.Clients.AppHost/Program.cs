using Projects;
using Serilog;
using UKHO.ADDS.Clients.AppHost.Constants;
using UKHO.ADDS.Clients.AppHost.Extensions;

namespace UKHO.ADDS.Clients.AppHost
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            Log.Information("ADDS Mocks for Clients Aspire Orchestrator");
            var builder = DistributedApplication.CreateBuilder(args);
            builder.AddProject<UKHO_ADDS_Mocks_Clients>(ProcessNames.MockService).WithDashboard("Dashboard");
            builder.Build().Run();
        }
    }
}
