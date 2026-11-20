
namespace SbomNuGetDownloader.Infrastructure;

public sealed class ConsoleLogger
{
	private readonly object _sync = new();

	public void Information(string message) => Write("INFO", message);

	public void Warning(string message) => Write("WARN", message);

	public void Error(string message) => Write("ERROR", message);

	public void Success(string message) => Write(" OK ", message);

	private void Write(string level, string message)
	{
		lock (_sync)
		{
			Console.WriteLine(
				$"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
		}
	}
}
