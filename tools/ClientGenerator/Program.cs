namespace ClientGenerator
{
    internal class Program
    {
        static readonly Dictionary<string, string> _specs = new()
        {
            //{ "ess-public.yaml", "UKHO.ADDS.Clients.ExchangeSets" },
            { "fss-public.yaml", "UKHO.ADDS.Clients.FileShare" },
            //{ "pks-public.yaml", "UKHO.ADDS.Clients.ProductKeys" },
            //{ "scs-1.10.yaml", "UKHO.ADDS.Clients.SalesCatalogue" },
        };

        private static async Task Main(string[] args)
        {
            var generator = new KiotaClientGenerator();

            foreach (var spec in _specs)
            {
                var specPath = $"Specs/{spec.Key}";
                var outputPath = $"../../../../../src/{spec.Value}";

                await generator.GenerateClientAsync(specPath, outputPath, "csharp");
            }
        }
    }
}
