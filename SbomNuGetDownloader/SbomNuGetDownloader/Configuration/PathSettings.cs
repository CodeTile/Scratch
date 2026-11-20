namespace SbomNuGetDownloader.Configuration;

public sealed class PathSettings
{
	public string SbomRootFolder { get; set; } = "";
	public string ExistingPackagesFolder { get; set; } = "";
	public string DownloadsFolder { get; set; } = "";
}
