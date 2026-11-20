
using System.Text.Json;

using SbomNuGetDownloader.Infrastructure;
using SbomNuGetDownloader.Models;

namespace SbomNuGetDownloader.Services;

public sealed class SbomScanner(ConsoleLogger logger)
{
	public IReadOnlyList<PackageReference> Scan(string rootFolder)
	{
		if (!Directory.Exists(rootFolder))
		{
			throw new DirectoryNotFoundException(
				$"SBOM root folder does not exist: {rootFolder}");
		}

		var files = Directory
			.EnumerateFiles(
				rootFolder,
				"sbom.json",
				SearchOption.AllDirectories)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		logger.Information($"Found {files.Length} sbom.json file(s).");

		var packages =
			new Dictionary<string, PackageReference>(
				StringComparer.OrdinalIgnoreCase);

		foreach (var file in files)
		{
			logger.Information($"Reading SBOM: {file}");

			using var document = JsonDocument.Parse(
				File.ReadAllText(file));

			if (document.RootElement.ValueKind != JsonValueKind.Object)
			{
				logger.Warning($"Ignoring invalid SBOM root: {file}");
				continue;
			}

			if (document.RootElement.TryGetProperty(
					"bomFormat", out var format) &&
				!string.Equals(
					format.GetString(),
					"CycloneDX",
					StringComparison.OrdinalIgnoreCase))
			{
				logger.Warning($"Ignoring non-CycloneDX document: {file}");
				continue;
			}

			if (document.RootElement.TryGetProperty(
					"components", out var components))
			{
				AddComponents(components, packages);
			}
		}

		logger.Information(
			$"Collected {packages.Count} unique NuGet package reference(s).");

		return packages.Values
			.OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
			.ThenBy(item => item.Version, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	private static void AddComponents(
		JsonElement components,
		IDictionary<string, PackageReference> packages)
	{
		if (components.ValueKind != JsonValueKind.Array)
		{
			return;
		}

		foreach (var component in components.EnumerateArray())
		{
			if (component.ValueKind != JsonValueKind.Object)
			{
				continue;
			}

			var package = ParseComponent(component);

			if (package is not null)
			{
				var key =
					$"{package.Id.ToLowerInvariant()}|{package.Version.ToLowerInvariant()}";

				packages[key] = package;
			}

			if (component.TryGetProperty(
					"components", out var nested))
			{
				AddComponents(nested, packages);
			}
		}
	}

	private static PackageReference? ParseComponent(
		JsonElement component)
	{
		var purl = GetString(component, "purl");

		if (string.IsNullOrWhiteSpace(purl))
		{
			return null;
		}

		var parsed = ParseNuGetPurl(purl);

		if (parsed is null)
		{
			return null;
		}

		var version = string.IsNullOrWhiteSpace(parsed.Value.Version)
			? GetString(component, "version")
			: parsed.Value.Version;

		if (string.IsNullOrWhiteSpace(version))
		{
			return null;
		}

		return new PackageReference(
			parsed.Value.Id.Trim(),
			version.Trim());
	}

	private static (string Id, string Version)? ParseNuGetPurl(
		string purl)
	{
		if (!purl.StartsWith(
				"pkg:nuget/",
				StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		var value = purl[10..].Split('?', '#')[0];

		var separator = value.IndexOf('@');

		var id = separator >= 0
			? value[..separator]
			: value;

		var version = separator >= 0
			? value[(separator + 1)..]
			: string.Empty;

		id = Uri.UnescapeDataString(id);
		version = Uri.UnescapeDataString(version);

		return string.IsNullOrWhiteSpace(id)
			? null
			: (id, version);
	}

	private static string? GetString(
		JsonElement element,
		string propertyName)
	{
		return element.TryGetProperty(
				   propertyName,
				   out var value) &&
			   value.ValueKind == JsonValueKind.String
			? value.GetString()
			: null;
	}
}
