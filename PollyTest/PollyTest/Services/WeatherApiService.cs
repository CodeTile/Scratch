using PollyTest.Models;

namespace PollyTest.Services;

public class WeatherApiService(IHttpClientFactory httpClientFactory) : BaseApiService(httpClientFactory), IWeatherApiService
{
	public async Task<IEnumerable<WeatherForecastDto>?> GetWeatherAsync()
	{
		return await GetAsync<IEnumerable<WeatherForecastDto>?>("WeatherForecast");
	}
}