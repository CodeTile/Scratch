
using NuGet.Versioning;

using SbomNuGetDownloader.Configuration;
using SbomNuGetDownloader.Infrastructure;
using SbomNuGetDownloader.Models;

namespace SbomNuGetDownloader.Services;

public sealed class NuGetDownloadService(
	ApplicationSettings settings,
	NuGetOrgClient nuGetClient,
	DownloadManifestService manifestService,
	ConsoleLogger logger)
{
	public async Task DownloadAsync(
		string manifestPath,
		DownloadManifest manifest,
		CancellationToken cancellationToken)
	{
		var downloadFolder = settings.Paths.DownloadsFolder;

		Directory.CreateDirectory(downloadFolder);

		var completed = 0;
		var failed = 0;

		foreach (var package in manifest.Packages.Where(p => p.Status != "Downloaded"))
		{
			cancellationToken.ThrowIfCancellationRequested();

			var destination = Path.Combine(
				downloadFolder,
				GetPackageFileName(package));

			if (settings.Downloads.SkipAlreadyDownloaded &&
				File.Exists(destination))
			{
				package.Status = "Downloaded";
				package.DownloadedFile = destination;
				package.Error = null;

				logger.Success(
					$"Already downloaded: {package.Id} {package.ResolvedVersion}");

				await manifestService.SaveAsync(
					manifestPath,
					manifest,
					cancellationToken);

				completed++;
				continue;
			}

			if (package.Status.Equals(
					"Failed",
					StringComparison.OrdinalIgnoreCase) &&
				!settings.Downloads.RetryFailedDownloads)
			{
				logger.Warning(
					$"Skipping failed package: {package.Id} {package.ResolvedVersion}");

				failed++;
				continue;
			}

			if (!NuGetVersion.TryParse(
					package.ResolvedVersion,
					out var version))
			{
				package.Status = "Failed";
				package.Error =
					$"Invalid version: {package.ResolvedVersion}";

				package.LastAttemptUtc = DateTimeOffset.UtcNow;

				failed++;

				await manifestService.SaveAsync(
					manifestPath,
					manifest,
					cancellationToken);

				continue;
			}

			var partialPath = destination + ".partial";

			try
			{
				package.Status = "Pending";
				package.Error = null;
				package.LastAttemptUtc = DateTimeOffset.UtcNow;

				await manifestService.SaveAsync(
					manifestPath,
					manifest,
					cancellationToken);

				logger.Information(
					$"Downloading {package.Id} {version}...");

				await nuGetClient.DownloadPackageAsync(
					package.Id,
					version,
					partialPath,
					cancellationToken);

				File.Move(
					partialPath,
					destination,
					overwrite: true);

				package.Status = "Downloaded";
				package.DownloadedFile = destination;
				package.Error = null;

				completed++;

				logger.Success(
					$"Downloaded: {package.Id} {version}");
			}
			catch (Exception exception) when (
				exception is HttpRequestException or
				IOException or
				InvalidDataException)
			{
				if (File.Exists(partialPath))
				{
					File.Delete(partialPath);
				}

				package.Status = "Failed";
				package.Error = exception.Message;
				package.DownloadedFile = null;
				package.LastAttemptUtc = DateTimeOffset.UtcNow;

				failed++;

				logger.Error(
					$"Download failed for {package.Id} {version}: {exception.Message}");
			}

			await manifestService.SaveAsync(
				manifestPath,
				manifest,
				cancellationToken);
		}

		logger.Information(
			$"Download finished. Downloaded/present: {completed}; failed/skipped: {failed}.");
	}

	private static string GetPackageFileName(
		ManifestPackage package)
	{
		var safeId = string.Concat(
			package.Id.Select(character =>
				Path.GetInvalidFileNameChars().Contains(character)
					? '_'
					: character));

		var safeVersion = string.Concat(
			package.ResolvedVersion.Select(character =>
				Path.GetInvalidFileNameChars().Contains(character)
					? '_'
					: character));

		return $"{safeId}.{safeVersion}.nupkg";
	}
}
