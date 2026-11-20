
using System.Text.Json;

using SbomNuGetDownloader.Models;

namespace SbomNuGetDownloader.Services;

public sealed class DownloadManifestService
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true
	};

	public async Task<DownloadManifest> LoadAsync(
		string path,
		CancellationToken cancellationToken)
	{
		await using var stream = new FileStream(
			path,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			81920,
			useAsync: true);

		return await JsonSerializer.DeserializeAsync<DownloadManifest>(
				   stream,
				   JsonOptions,
				   cancellationToken)
			   ?? throw new InvalidDataException(
				   $"The download manifest is empty or invalid: {path}");
	}

	public async Task SaveAsync(
		string path,
		DownloadManifest manifest,
		CancellationToken cancellationToken)
	{
		var directory = Path.GetDirectoryName(path)
			?? throw new InvalidOperationException(
				"Manifest path has no parent directory.");

		Directory.CreateDirectory(directory);

		var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

		try
		{
			await using (var stream = new FileStream(
							 temporaryPath,
							 FileMode.CreateNew,
							 FileAccess.Write,
							 FileShare.None,
							 81920,
							 useAsync: true))
			{
				await JsonSerializer.SerializeAsync(
					stream,
					manifest,
					JsonOptions,
					cancellationToken);

				await stream.FlushAsync(cancellationToken);
			}

			File.Move(
				temporaryPath,
				path,
				overwrite: true);
		}
		finally
		{
			if (File.Exists(temporaryPath))
			{
				File.Delete(temporaryPath);
			}
		}
	}
}
