using Microsoft.AspNetCore.Mvc;

namespace PollyTest.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
	private static readonly string[] Summaries =
	[
		"Ping Pong","Foo Bar","Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
	];

	[HttpGet]
	public async Task<ActionResult<IEnumerable<WeatherForecastDto>>> GetAsync()
	{
		var forecasts = Enumerable.Range(1, 5)
			.Select(index => new WeatherForecastDto
			{
				Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
				TemperatureC = Random.Shared.Next(-20, 55),
				Summary = Summaries[Random.Shared.Next(Summaries.Length)]
			})
			.ToArray();

		return Ok(forecasts);
	}
}
