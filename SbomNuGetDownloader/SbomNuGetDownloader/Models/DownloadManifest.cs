namespace SbomNuGetDownloader.Models;

public sealed class DownloadManifest
{
	public int SchemaVersion { get; set; } = 1;

	public DateTimeOffset CreatedUtc { get; set; } =
		DateTimeOffset.UtcNow;

	public bool UseLatestVersions { get; set; }

	public List<ManifestPackage> Packages { get; set; } = [];
	public bool DownloadBothVersions { get; internal set; }
}
