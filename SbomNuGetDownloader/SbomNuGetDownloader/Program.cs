
using System.Text.Json;

using SbomNuGetDownloader.Configuration;
using SbomNuGetDownloader.Infrastructure;
using SbomNuGetDownloader.Services;

namespace NuGetSbomDownloader;

internal static class Program
{
	private static readonly JsonSerializerOptions ConfigurationJsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public static async Task<int> Main(string[] args)
	{
		using var cancellationSource = new CancellationTokenSource();

		Console.CancelKeyPress += (_, eventArgs) =>
		{
			eventArgs.Cancel = true;
			cancellationSource.Cancel();
		};

		try
		{
			if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) ||
				args.Contains("-h", StringComparer.OrdinalIgnoreCase))
			{
				PrintUsage();
				return 0;
			}

			var regenerateManifest = args.Contains(
				"--regenerate-manifest",
				StringComparer.OrdinalIgnoreCase);

			var settingsPath = GetOptionValue(args, "--settings")
				?? Path.Combine(
					AppContext.BaseDirectory,
					"appsettings.json");

			if (!File.Exists(settingsPath))
			{
				throw new FileNotFoundException(
					$"Configuration file was not found: {settingsPath}");
			}

			var settingsJson = await File.ReadAllTextAsync(
				settingsPath,
				cancellationSource.Token);

			var settings = JsonSerializer.Deserialize<ApplicationSettings>(
				settingsJson,
				ConfigurationJsonOptions)
				?? throw new InvalidDataException(
					$"Could not read configuration file: {settingsPath}");

			ValidateSettings(settings);

			var logger = new ConsoleLogger();

			var manifestPath = Path.Combine(
				settings.Paths.DownloadsFolder,
				settings.Downloads.ManifestFileName);

			Directory.CreateDirectory(
				settings.Paths.DownloadsFolder);

			using var httpClient = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(
					settings.Downloads.RequestTimeoutSeconds)
			};

			httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
				"NuGetSbomDownloader/1.0 (.NET 10)");

			var nuGetClient = new NuGetOrgClient(
				httpClient,
				settings.NuGet.Source);

			var manifestService = new DownloadManifestService();

			var downloader = new NuGetDownloadService(
				settings,
				nuGetClient,
				manifestService,
				logger);

			if (File.Exists(manifestPath) && !regenerateManifest)
			{
				logger.Information(
					$"Existing master manifest found: {manifestPath}");

				logger.Information(
					"SBOM scanning and package comparison are skipped.");

				var existingManifest = await manifestService.LoadAsync(
					manifestPath,
					cancellationSource.Token);

				await downloader.DownloadAsync(
					manifestPath,
					existingManifest,
					cancellationSource.Token);

				return 0;
			}

			if (File.Exists(manifestPath) && regenerateManifest)
			{
				logger.Warning(
					"Manifest regeneration requested. Existing manifest will be replaced.");
			}

			var scanner = new SbomScanner(logger);

			var packageScanner = new NuGetPackageScanner(logger);

			var frameworkInspector = new PackageFrameworkInspector();

			var builder = new ManifestBuilder(
				settings,
				scanner,
				packageScanner,
				nuGetClient,
				frameworkInspector,
				logger);

			var manifest = await builder.BuildAsync(
				cancellationSource.Token);

			await manifestService.SaveAsync(
				manifestPath,
				manifest,
				cancellationSource.Token);

			logger.Success(
				$"Master manifest saved: {manifestPath}");

			await downloader.DownloadAsync(
				manifestPath,
				manifest,
				cancellationSource.Token);

			return 0;
		}
		catch (OperationCanceledException)
		{
			Console.WriteLine("Operation cancelled.");
			return 130;
		}
		catch (Exception exception)
		{
			Console.Error.WriteLine(
				$"Fatal error: {exception.Message}");

			Console.Error.WriteLine(exception);

			return 1;
		}
	}

	private static string? GetOptionValue(
		string[] args,
		string option)
	{
		for (var index = 0; index < args.Length - 1; index++)
		{
			if (args[index].Equals(
					option,
					StringComparison.OrdinalIgnoreCase))
			{
				return args[index + 1];
			}
		}

		return null;
	}

	private static void ValidateSettings(
		ApplicationSettings settings)
	{
		if (string.IsNullOrWhiteSpace(
				settings.Paths.SbomRootFolder) ||
			string.IsNullOrWhiteSpace(
				settings.Paths.ExistingPackagesFolder) ||
			string.IsNullOrWhiteSpace(
				settings.Paths.DownloadsFolder))
		{
			throw new InvalidOperationException(
				"All three folder paths must be configured.");
		}

		if (!Uri.TryCreate(
				settings.NuGet.Source,
				UriKind.Absolute,
				out _))
		{
			throw new InvalidOperationException(
				"NuGet.Source must be an absolute URL.");
		}

		if (settings.Downloads.RequestTimeoutSeconds <= 0)
		{
			throw new InvalidOperationException(
				"RequestTimeoutSeconds must be greater than zero.");
		}

		if (string.IsNullOrWhiteSpace(
				settings.Downloads.ManifestFileName) ||
			Path.GetFileName(
				settings.Downloads.ManifestFileName) !=
			settings.Downloads.ManifestFileName)
		{
			throw new InvalidOperationException(
				"ManifestFileName must be a filename, not a path.");
		}

		if (!settings.NuGet.Source.Equals(
				"https://api.nuget.org/v3/index.json",
				StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException(
				"Only the official NuGet.org v3 source is supported.");
		}

		foreach (var framework in settings.FrameworkFiltering.ExcludedFrameworks)
		{
			var parsed = NuGet.Frameworks.NuGetFramework.ParseFolder(framework);

			if (parsed.IsUnsupported)
			{
				throw new InvalidOperationException(
					$"Unsupported excluded framework: {framework}");
			}
		}
	}

	private static void PrintUsage()
	{
		Console.WriteLine("NuGetSbomDownloader");
		Console.WriteLine();

		Console.WriteLine("Usage:");
		Console.WriteLine(
			"  NuGetSbomDownloader [--settings <path>] [--regenerate-manifest]");

		Console.WriteLine();

		Console.WriteLine("Options:");
		Console.WriteLine(
			"  --settings <path>       Use an alternative configuration file.");

		Console.WriteLine(
			"  --regenerate-manifest   Rebuild downloads.json from SBOM files.");

		Console.WriteLine(
			"  --help, -h              Display help.");

		Console.WriteLine();

		Console.WriteLine(
			"An existing manifest is reused unless regeneration is requested.");
	}
}
