using PollyTest.Services;

namespace PollyTest.Tests;

public class TestApiService : BaseApiService
{
	public TestApiService(IHttpClientFactory factory)
		: base(factory)
	{
	}

	public Task<TestDto?> GetAsyncTest(string url)
		=> GetAsync<TestDto>(url);

	public Task<TestDto?> PostAsyncTest(string url, CreateDto dto)
		=> PostAsync<CreateDto, TestDto>(url, dto);

	public Task PutAsyncTest(string url, UpdateDto dto)
		=> PutAsync(url, dto);

	public Task DeleteAsyncTest(string url)
		=> DeleteAsync(url);
}