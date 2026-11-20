namespace SbomNuGetDownloader.Configuration;

public sealed class FrameworkFilteringSettings
{
	public bool Enabled { get; set; } = true;

	public List<string> ExcludedFrameworks { get; set; } = [];
}
