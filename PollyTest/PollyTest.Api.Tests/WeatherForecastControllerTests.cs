using Microsoft.AspNetCore.Mvc;

using PollyTest.Api.Controllers;

using Shouldly;

namespace PollyTest.Api.Tests
{
	public class WeatherForecastControllerTests
	{
		[Fact]
		public async Task WeatherForecastController_GetAsync_ShouldReturnFiveForecastsTests()
		{
			/// Arrange
			var uot = new WeatherForecastController();

			/// Act
			var result = await uot.GetAsync();

			/// Assert
			var okResult = result.Result as OkObjectResult;

			okResult.ShouldNotBeNull();

			var data = okResult!.Value as IEnumerable<WeatherForecastDto>;

			data.ShouldNotBeNull();
			data!.Count().ShouldBe(5);
		}

		[Fact]
		public async Task WeatherForecastController_GetAsync_ShouldReturnWeatherForecastItemsTests()
		{
			/// Arrange
			var uot = new WeatherForecastController();

			/// Act
			var result = await uot.GetAsync();

			/// Assert
			var okResult = result.Result as OkObjectResult;

			okResult.ShouldNotBeNull();
			var data = okResult!.Value as IEnumerable<WeatherForecastDto>;

			data.ShouldNotBeNull();
			data!.All(x => x.Summary != null).ShouldBeTrue();
		}
	}
}
