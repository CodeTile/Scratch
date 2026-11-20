namespace SbomNuGetDownloader.Models;

public sealed class ManifestPackage
{
	public required string Id { get; set; }

	public required string RequestedVersion { get; set; }

	public required string ResolvedVersion { get; set; }

	public string Status { get; set; } = "Pending";

	public string? DownloadedFile { get; set; }

	public string? Error { get; set; }

	public DateTimeOffset? LastAttemptUtc { get; set; }
}
