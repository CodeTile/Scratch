namespace SbomNuGetDownloader.Configuration;


public sealed class ApplicationSettings
{
	public PathSettings Paths { get; set; } = new();
	public NuGetSettings NuGet { get; set; } = new();
	public FrameworkFilteringSettings FrameworkFiltering { get; set; } = new();
	public DownloadSettings Downloads { get; set; } = new();
}
