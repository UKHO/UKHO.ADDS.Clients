namespace UKHO.ADDS.Mocks.Clients
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            MockServices.AddServices();

            await MockServer.RunAsync(args);
        }
    }
}
