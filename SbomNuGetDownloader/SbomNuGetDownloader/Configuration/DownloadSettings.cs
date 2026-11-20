namespace SbomNuGetDownloader.Configuration;

public sealed class DownloadSettings
{
	public string ManifestFileName { get; set; } =
		"downloads.json";

	public bool SkipAlreadyDownloaded { get; set; } = true;

	public bool RetryFailedDownloads { get; set; } = true;

	public int RequestTimeoutSeconds { get; set; } = 180; 
	public bool ForceDownload { get; set; }
}
