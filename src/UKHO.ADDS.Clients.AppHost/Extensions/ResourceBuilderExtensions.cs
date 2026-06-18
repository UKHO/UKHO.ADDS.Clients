using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UKHO.ADDS.Clients.AppHost.Extensions
{
    internal static class ResourceBuilderExtensions
    {
        internal static IResourceBuilder<T> WithDashboard<T>(this IResourceBuilder<T> builder, string displayName) where T : IResourceWithEndpoints => builder.WithDashboard(displayName, "adds-mock-dashboard");

        internal static IResourceBuilder<T> WithDashboard<T>(this IResourceBuilder<T> builder, string displayName, string name)
            where T : IResourceWithEndpoints =>
            builder.WithCommand(
                name,
                displayName,
                async context =>
                {
                    try
                    {
                        var endpoint = builder.GetEndpoint("https");
                        var url = $"{endpoint.Url}";

                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

                        return await Task.FromResult(new ExecuteCommandResult { Success = true });
                    }
                    catch (Exception e)
                    {
                        return new ExecuteCommandResult { Success = false, ErrorMessage = e.ToString() };
                    }
                },
                new CommandOptions { UpdateState = context => { return context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy ? ResourceCommandState.Enabled : ResourceCommandState.Disabled; }, IconName = "Document", IconVariant = IconVariant.Filled });
    }
}
