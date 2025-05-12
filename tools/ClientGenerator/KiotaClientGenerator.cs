using CliWrap;
using CliWrap.Buffered;
using CliWrap.EventStream;

namespace ClientGenerator
{
    public class KiotaClientGenerator
    {
        /// <summary>
        /// Invokes the Kiota CLI tool to generate code from an OpenAPI spec and streams output to the console.
        /// </summary>
        /// <param name="specFilePath">Path to the OpenAPI YAML file (must be a local path).</param>
        /// <param name="outputDirectory">Path to the output directory where code should be generated.</param>
        /// <param name="language">The target language (e.g., "csharp", "typescript", "java").</param>
        public async Task GenerateClientAsync(string specFilePath, string outputDirectory, string language = "csharp")
        {
            if (string.IsNullOrWhiteSpace(specFilePath))
                throw new ArgumentException("OpenAPI spec path is required.", nameof(specFilePath));

            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Output path is required.", nameof(outputDirectory));

            if (!File.Exists(specFilePath))
                throw new FileNotFoundException("The OpenAPI spec file was not found.", specFilePath);

            var kiotaExecutable = "kiota"; // Assumes kiota is on the PATH
            var uri = new Uri(Path.GetFullPath(specFilePath)).AbsoluteUri;

            var cmd = Cli.Wrap(kiotaExecutable)
                .WithArguments(args => args
                    .Add("generate")
                    .Add("--language").Add(language)
                    .Add("--openapi").Add(specFilePath)
                    .Add("--output").Add(outputDirectory));

            await foreach (var cmdEvent in cmd.ListenAsync())
            {
                switch (cmdEvent)
                {
                    case StandardOutputCommandEvent stdOut:
                        Console.Out.WriteLine(stdOut.Text);
                        break;

                    case StandardErrorCommandEvent stdErr:
                        Console.Error.WriteLine(stdErr.Text);
                        break;

                    case ExitedCommandEvent exited when exited.ExitCode != 0:
                        throw new InvalidOperationException($"Kiota exited with code {exited.ExitCode}.");
                }
            }
        }
    }
}
