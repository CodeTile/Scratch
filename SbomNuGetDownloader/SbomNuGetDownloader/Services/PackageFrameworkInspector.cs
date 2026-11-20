
using NuGet.Frameworks;
using NuGet.Packaging;

namespace SbomNuGetDownloader.Services;

public sealed class PackageFrameworkInspector
{
	private static readonly HashSet<string> FrameworkAssetRoots =
		new(StringComparer.OrdinalIgnoreCase)
		{
			"lib",
			"ref",
			"build",
			"buildTransitive",
			"buildMultiTargeting"
		};

	public FrameworkInspection Inspect(
		string packageFile,
		IReadOnlyCollection<string> excludedFrameworks)
	{
		using var reader = new PackageArchiveReader(packageFile);

		var frameworks = new HashSet<NuGetFramework>();

		foreach (var path in reader.GetFiles())
		{
			var segments = path
				.Replace('\\', '/')
				.Split('/', StringSplitOptions.RemoveEmptyEntries);

			var frameworkFolder = FindFrameworkFolder(segments);

			if (frameworkFolder is null)
			{
				continue;
			}

			var framework = NuGetFramework.ParseFolder(
				frameworkFolder);

			if (!framework.IsUnsupported && !framework.IsAny)
			{
				frameworks.Add(framework);
			}
		}

		if (frameworks.Count == 0)
		{
			return new FrameworkInspection(
				true,
				[],
				"No framework-specific assets found. Package is framework-agnostic.");
		}

		var exclusions = excludedFrameworks
			.Select(NuGetFramework.ParseFolder)
			.Where(framework => !framework.IsUnsupported)
			.ToArray();

		var rejected = frameworks
			.Where(candidate =>
				exclusions.Any(excluded =>
					SameFrameworkFamilyAndVersion(
						candidate,
						excluded)))
			.ToArray();

		var accepted = frameworks
			.Except(rejected)
			.ToArray();

		var isAllowed = accepted.Length > 0;

		var detected = frameworks
			.Select(framework => framework.GetShortFolderName())
			.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		var reason = isAllowed
			? $"Allowed frameworks: {string.Join(", ", accepted.Select(item => item.GetShortFolderName()))}."
			: $"All detected frameworks are excluded: {string.Join(", ", detected)}.";

		return new FrameworkInspection(
			isAllowed,
			detected,
			reason);
	}

	private static string? FindFrameworkFolder(string[] segments)
	{
		if (segments.Length < 2)
		{
			return null;
		}

		if (FrameworkAssetRoots.Contains(segments[0]))
		{
			return segments[1];
		}

		if (segments[0].Equals(
				"runtimes",
				StringComparison.OrdinalIgnoreCase))
		{
			for (var index = 2; index < segments.Length - 1; index++)
			{
				if (segments[index].Equals(
						"lib",
						StringComparison.OrdinalIgnoreCase) ||
					segments[index].Equals(
						"ref",
						StringComparison.OrdinalIgnoreCase))
				{
					return segments[index + 1];
				}
			}
		}

		if (segments[0].Equals(
				"contentFiles",
				StringComparison.OrdinalIgnoreCase) &&
			segments.Length >= 3)
		{
			return segments[2];
		}

		return null;
	}

	private static bool SameFrameworkFamilyAndVersion(
		NuGetFramework candidate,
		NuGetFramework excluded)
	{
		return string.Equals(
				   candidate.Framework,
				   excluded.Framework,
				   StringComparison.OrdinalIgnoreCase) &&
			   candidate.Version == excluded.Version;
	}
}

public sealed record FrameworkInspection(
	bool IsAllowed,
	IReadOnlyList<string> DetectedFrameworks,
	string Reason);
