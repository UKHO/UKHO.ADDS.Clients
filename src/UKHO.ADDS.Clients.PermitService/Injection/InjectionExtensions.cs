using Microsoft.Extensions.DependencyInjection;

namespace UKHO.ADDS.Clients.PermitService.Injection
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AddPermitClient(this IServiceCollection collection)
        {
            collection.AddTransient<IPermitClientFactory, PermitClientFactory>();

            return collection;
        }
    }
}
