namespace PollyTest.Services;

public abstract class BaseApiService
{
	private readonly IHttpClientFactory _httpClientFactory;

	protected BaseApiService(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	protected HttpClient Client =>
		_httpClientFactory.CreateClient("MyApi");

	protected async Task<T?> GetAsync<T>(string url)
	{
		var response = await Client.GetAsync(url);

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<T>();
	}

	protected async Task<TResult?> PostAsync<TRequest, TResult>(
		string url,
		TRequest data)
	{
		var response = await Client.PostAsJsonAsync(url, data);

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<TResult>();
	}

	protected async Task PutAsync<TRequest>(
		string url,
		TRequest data)
	{
		var response = await Client.PutAsJsonAsync(url, data);

		response.EnsureSuccessStatusCode();
	}

	protected async Task DeleteAsync(string url)
	{
		var response = await Client.DeleteAsync(url);

		response.EnsureSuccessStatusCode();
	}
}
