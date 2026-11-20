namespace SbomNuGetDownloader.Configuration;

public sealed class NuGetSettings
{
	public string Source { get; set; } =
		"https://api.nuget.org/v3/index.json";

	public bool UseLatestVersions { get; set; }

	public bool DownloadBothVersions { get; set; }

	public bool IncludePrerelease { get; set; }
}
