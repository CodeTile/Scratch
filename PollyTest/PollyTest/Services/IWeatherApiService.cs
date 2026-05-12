using PollyTest.Models;

namespace PollyTest.Services;

public interface IWeatherApiService
{
	Task<IEnumerable<WeatherForecastDto>?> GetWeatherAsync();
}