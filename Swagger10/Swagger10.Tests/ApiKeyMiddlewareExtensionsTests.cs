namespace Swagger10.Tests;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Swagger10.Startup;

using Xunit;

public class ApiKeyMiddlewareExtensionsTests
{
	[Fact]
	public void UseApiKey_Returns_IApplicationBuilder()
	{
		// Arrange
		var services = new ServiceCollection();
		var provider = services.BuildServiceProvider();
		var appBuilder = new ApplicationBuilder(provider);

		// Act
		var result = appBuilder.UseApiKey();

		// Assert
		Assert.Same(appBuilder, result);
	}

	[Fact]
	public async Task UseApiKey_Registers_ApiKeyMiddleware_In_Pipeline()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "ApiKey", "test-key" }
			})
			.Build());

		var provider = services.BuildServiceProvider();
		var appBuilder = new ApplicationBuilder(provider);

		bool middlewareCalled = false;

		// Add the extension method
		appBuilder.UseApiKey();

		// Add a terminal middleware to detect pipeline continuation
		appBuilder.Run(context =>
		{
			middlewareCalled = true;
			return Task.CompletedTask;
		});

		var app = appBuilder.Build();

		// Act
		var context = new DefaultHttpContext();
		context.Request.Path = "/weather";
		context.Request.Headers["X-Api-Key"] = "test-key";

		await app(context);

		// Assert
		Assert.True(middlewareCalled);
	}

	[Fact]
	public async Task UseApiKey_Triggers_401_When_ApiKey_Missing()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "ApiKey", "test-key" }
			})
			.Build());

		var provider = services.BuildServiceProvider();
		var appBuilder = new ApplicationBuilder(provider);

		appBuilder.UseApiKey();
		appBuilder.Run(_ => Task.CompletedTask);

		var app = appBuilder.Build();

		var context = new DefaultHttpContext();
		context.Request.Path = "/weather";

		// Act
		await app(context);

		// Assert
		Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
	}
}

