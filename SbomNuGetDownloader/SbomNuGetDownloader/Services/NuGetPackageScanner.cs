
using NuGet.Packaging;

using SbomNuGetDownloader.Infrastructure;

namespace SbomNuGetDownloader.Services;

public sealed class NuGetPackageScanner(ConsoleLogger logger)
{
	public IReadOnlySet<string> Scan(string folder)
	{
		if (!Directory.Exists(folder))
		{
			logger.Warning(
				$"Existing packages folder does not exist: {folder}");

			return new HashSet<string>(
				StringComparer.OrdinalIgnoreCase);
		}

		var identities = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase);

		foreach (var file in Directory.EnumerateFiles(
					 folder,
					 "*.nupkg",
					 SearchOption.AllDirectories)
				 .Where(path =>
					 !path.EndsWith(
						 ".snupkg",
						 StringComparison.OrdinalIgnoreCase)))
		{
			try
			{
				using var reader = new PackageArchiveReader(file);

				var identity = reader.GetIdentity();

				identities.Add(Key(
					identity.Id,
					identity.Version.ToNormalizedString()));
			}
			catch (Exception exception) when (
				exception is InvalidDataException or
				IOException or
				ArgumentException)
			{
				logger.Warning(
					$"Could not read package '{file}': {exception.Message}");
			}
		}

		logger.Information(
			$"Found {identities.Count} existing NuGet package(s).");

		return identities;
	}

	public static string Key(string id, string version)
	{
		return $"{id.Trim().ToLowerInvariant()}|{version.Trim().ToLowerInvariant()}";
	}
}
