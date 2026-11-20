
using NuGet.Versioning;

using SbomNuGetDownloader.Configuration;
using SbomNuGetDownloader.Infrastructure;
using SbomNuGetDownloader.Models;

namespace SbomNuGetDownloader.Services;

public sealed class ManifestBuilder(
	ApplicationSettings settings,
	SbomScanner sbomScanner,
	NuGetPackageScanner packageScanner,
	NuGetOrgClient nuGetClient,
	PackageFrameworkInspector frameworkInspector,
	ConsoleLogger logger)
{
	public async Task<DownloadManifest> BuildAsync(
		CancellationToken cancellationToken)
	{
		var references = sbomScanner.Scan(
			settings.Paths.SbomRootFolder);

		var existing = packageScanner.Scan(
			settings.Paths.ExistingPackagesFolder);

		var manifest = new DownloadManifest
		{
			UseLatestVersions = settings.NuGet.UseLatestVersions,
			DownloadBothVersions = settings.NuGet.DownloadBothVersions
		};

		var temporaryFolder = Path.Combine(
			Path.GetTempPath(),
			"NuGetSbomDownloader",
			Guid.NewGuid().ToString("N"));

		Directory.CreateDirectory(temporaryFolder);

		var manifestPackages = new Dictionary<string, ManifestPackage>(
			StringComparer.OrdinalIgnoreCase);

		try
		{
			var packageGroups = references
				.GroupBy(
					package => package.Id,
					StringComparer.OrdinalIgnoreCase)
				.OrderBy(
					group => group.Key,
					StringComparer.OrdinalIgnoreCase);

			foreach (var group in packageGroups)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var packageId = group.Key;

				logger.Information(
					$"Processing package: {packageId}");

				var selectedVersions = new Dictionary<string, string>(
					StringComparer.OrdinalIgnoreCase);

				if (settings.NuGet.DownloadBothVersions)
				{
					logger.Information(
						$"Resolving SBOM and latest versions for {packageId}.");

					foreach (var reference in group)
					{
						var exactVersion = await ResolveExactAsync(
							reference,
							temporaryFolder,
							cancellationToken);

						if (exactVersion is not null)
						{
							AddVersion(
								selectedVersions,
								exactVersion,
								reference.Version);
						}
					}

					var latestVersion = await ResolveLatestCompatibleAsync(
						packageId,
						temporaryFolder,
						cancellationToken);

					if (latestVersion is not null)
					{
						AddVersion(
							selectedVersions,
							latestVersion,
							"latest");
					}
				}
				else if (settings.NuGet.UseLatestVersions)
				{
					logger.Information(
						$"Resolving latest compatible version for {packageId}.");

					var latestVersion = await ResolveLatestCompatibleAsync(
						packageId,
						temporaryFolder,
						cancellationToken);

					if (latestVersion is not null)
					{
						AddVersion(
							selectedVersions,
							latestVersion,
							"latest");
					}
				}
				else
				{
					foreach (var reference in group)
					{
						var exactVersion = await ResolveExactAsync(
							reference,
							temporaryFolder,
							cancellationToken);

						if (exactVersion is not null)
						{
							AddVersion(
								selectedVersions,
								exactVersion,
								reference.Version);
						}
					}
				}

				foreach (var selected in selectedVersions)
				{
					cancellationToken.ThrowIfCancellationRequested();

					var version = selected.Key;
					var requestedVersion = selected.Value;

					var identity = NuGetPackageScanner.Key(
						packageId,
						version);

					if (existing.Contains(identity) &&	!settings.Downloads.ForceDownload)
					{
						logger.Success(
							$"Already present: {packageId} {version}");

						continue;
					}

					if (existing.Contains(identity) &&
						settings.Downloads.ForceDownload)
					{
						logger.Information(
							$"Force download enabled: {packageId} {version}");
					}

					if (manifestPackages.ContainsKey(identity))
					{
						continue;
					}

					manifestPackages.Add(
						identity,
						new ManifestPackage
						{
							Id = packageId,
							RequestedVersion = requestedVersion,
							ResolvedVersion = version,
							Status = "Pending"
						});

					logger.Information(
						$"Added to download list: {packageId} {version}");
				}
			}
		}
		finally
		{
			if (Directory.Exists(temporaryFolder))
			{
				Directory.Delete(
					temporaryFolder,
					recursive: true);
			}
		}

		manifest.Packages = manifestPackages.Values
			.OrderBy(
				package => package.Id,
				StringComparer.OrdinalIgnoreCase)
			.ThenBy(
				package => NuGetVersion.Parse(
					package.ResolvedVersion))
			.ToList();

		logger.Success(
			$"Manifest generated with {manifest.Packages.Count} package version(s) to download.");

		return manifest;
	}

	private static void AddVersion(
		IDictionary<string, string> selectedVersions,
		NuGetVersion version,
		string requestedVersion)
	{
		var normalizedVersion = version.ToNormalizedString();

		if (!selectedVersions.ContainsKey(normalizedVersion))
		{
			selectedVersions.Add(
				normalizedVersion,
				requestedVersion);
		}
	}

	private async Task<NuGetVersion?> ResolveExactAsync(
		PackageReference reference,
		string temporaryFolder,
		CancellationToken cancellationToken)
	{
		if (!NuGetVersion.TryParse(
				reference.Version,
				out var version))
		{
			logger.Warning(
				$"Invalid version '{reference.Version}' for {reference.Id}.");

			return null;
		}

		if (!settings.NuGet.IncludePrerelease &&
			version.IsPrerelease)
		{
			logger.Warning(
				$"Skipping prerelease: {reference.Id} {version}");

			return null;
		}

		if (!settings.FrameworkFiltering.Enabled)
		{
			return version;
		}

		var temporaryFile = Path.Combine(
			temporaryFolder,
			$"{Guid.NewGuid():N}.nupkg");

		try
		{
			await nuGetClient.DownloadPackageAsync(
				reference.Id,
				version,
				temporaryFile,
				cancellationToken);

			var inspection = frameworkInspector.Inspect(
				temporaryFile,
				settings.FrameworkFiltering.ExcludedFrameworks);

			if (!inspection.IsAllowed)
			{
				logger.Warning(
					$"Excluded {reference.Id} {version}: {inspection.Reason}");

				return null;
			}

			return version;
		}
		catch (Exception exception) when (
			exception is HttpRequestException or
			IOException or
			InvalidDataException)
		{
			logger.Warning(
				$"Could not validate {reference.Id} {version}: {exception.Message}");

			return null;
		}
		finally
		{
			if (File.Exists(temporaryFile))
			{
				File.Delete(temporaryFile);
			}
		}
	}

	private async Task<NuGetVersion?> ResolveLatestCompatibleAsync(
		string packageId,
		string temporaryFolder,
		CancellationToken cancellationToken)
	{
		var versions = await nuGetClient.GetVersionsAsync(
			packageId,
			cancellationToken);

		foreach (var version in versions)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!settings.NuGet.IncludePrerelease &&
				version.IsPrerelease)
			{
				continue;
			}

			var temporaryFile = Path.Combine(
				temporaryFolder,
				$"{Guid.NewGuid():N}.nupkg");

			try
			{
				await nuGetClient.DownloadPackageAsync(
					packageId,
					version,
					temporaryFile,
					cancellationToken);

				if (!settings.FrameworkFiltering.Enabled)
				{
					logger.Success(
						$"Selected latest version: {packageId} {version}");

					return version;
				}

				var inspection = frameworkInspector.Inspect(
					temporaryFile,
					settings.FrameworkFiltering.ExcludedFrameworks);

				if (inspection.IsAllowed)
				{
					logger.Success(
						$"Selected latest compatible version: {packageId} {version}. {inspection.Reason}");

					return version;
				}

				logger.Information(
					$"Version {version} is incompatible. Trying an older version.");
			}
			catch (Exception exception) when (
				exception is HttpRequestException or
				IOException or
				InvalidDataException)
			{
				logger.Warning(
					$"Could not inspect {packageId} {version}: {exception.Message}");
			}
			finally
			{
				if (File.Exists(temporaryFile))
				{
					File.Delete(temporaryFile);
				}
			}
		}

		logger.Warning(
			$"No stable compatible version found for {packageId}.");

		return null;
	}
}
