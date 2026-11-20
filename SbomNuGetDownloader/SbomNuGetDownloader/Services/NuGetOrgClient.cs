
using System.Net;
using System.Text.Json;

using NuGet.Versioning;

namespace SbomNuGetDownloader.Services;

public sealed class NuGetOrgClient(
	HttpClient httpClient,
	string serviceIndexUrl)
{
	private readonly Uri _flatContainerBase =
		BuildFlatContainerUri(serviceIndexUrl);

	public async Task<IReadOnlyList<NuGetVersion>> GetVersionsAsync(
		string packageId,
		CancellationToken cancellationToken)
	{
		var url = new Uri(
			_flatContainerBase,
			$"{Uri.EscapeDataString(packageId.ToLowerInvariant())}/index.json");

		using var response = await httpClient.GetAsync(
			url,
			HttpCompletionOption.ResponseHeadersRead,
			cancellationToken);

		response.EnsureSuccessStatusCode();

		await using var stream =
			await response.Content.ReadAsStreamAsync(cancellationToken);

		using var document = await JsonDocument.ParseAsync(
			stream,
			cancellationToken: cancellationToken);

		if (!document.RootElement.TryGetProperty(
				"versions",
				out var versions) ||
			versions.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidDataException(
				$"NuGet.org returned an invalid version list for '{packageId}'.");
		}

		return versions
			.EnumerateArray()
			.Where(item => item.ValueKind == JsonValueKind.String)
			.Select(item =>
				NuGetVersion.TryParse(item.GetString(), out var version)
					? version
					: null)
			.Where(version => version is not null)
			.Cast<NuGetVersion>()
			.OrderByDescending(version => version)
			.ToArray();
	}

	public async Task DownloadPackageAsync(
		string packageId,
		NuGetVersion version,
		string destination,
		CancellationToken cancellationToken)
	{
		var id = packageId.ToLowerInvariant();

		var versionText = version
			.ToNormalizedString()
			.ToLowerInvariant();

		var url = new Uri(
			_flatContainerBase,
			$"{Uri.EscapeDataString(id)}/{Uri.EscapeDataString(versionText)}/{Uri.EscapeDataString(id)}.{Uri.EscapeDataString(versionText)}.nupkg");

		using var response = await httpClient.GetAsync(
			url,
			HttpCompletionOption.ResponseHeadersRead,
			cancellationToken);

		if (response.StatusCode == HttpStatusCode.NotFound)
		{
			throw new FileNotFoundException(
				$"NuGet.org does not contain {packageId} {versionText}.");
		}

		response.EnsureSuccessStatusCode();

		await using var source =
			await response.Content.ReadAsStreamAsync(cancellationToken);

		await using var target = new FileStream(
			destination,
			FileMode.Create,
			FileAccess.Write,
			FileShare.None,
			81920,
			useAsync: true);

		await source.CopyToAsync(target, cancellationToken);

		await target.FlushAsync(cancellationToken);
	}

	private static Uri BuildFlatContainerUri(string serviceIndexUrl)
	{
		var parsed = new Uri(serviceIndexUrl, UriKind.Absolute);

		if (!parsed.Host.Equals(
				"api.nuget.org",
				StringComparison.OrdinalIgnoreCase) ||
			!parsed.AbsolutePath.Equals(
				"/v3/index.json",
				StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException(
				"Only https://api.nuget.org/v3/index.json is supported.");
		}

		return new Uri(
			"https://api.nuget.org/v3-flatcontainer/");
	}
}
