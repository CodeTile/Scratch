namespace Swagger10.Tests;

using AutoFixture;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Swagger10.Startup;

using Xunit;

public class ApiKeyMiddlewareExtensions_AutoFixtureTests
{
	private readonly Fixture _fixture = new();

	private IApplicationBuilder CreateBuilderWithConfig(string apiKey)
	{
		var services = new ServiceCollection();

		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "ApiKey", apiKey }
			})
			.Build();

		services.AddSingleton<IConfiguration>(config);

		return new ApplicationBuilder(services.BuildServiceProvider());
	}

	[Fact]
	public void UseApiKey_ReturnsSameBuilder()
	{
		var apiKey = _fixture.Create<string>();
		var builder = CreateBuilderWithConfig(apiKey);

		var result = builder.UseApiKey();

		Assert.Same(builder, result);
	}

	[Fact]
	public async Task UseApiKey_AddsMiddleware_And_AllowsValidKey()
	{
		var apiKey = _fixture.Create<string>();
		var builder = CreateBuilderWithConfig(apiKey);

		bool nextCalled = false;

		builder.UseApiKey();
		builder.Run(_ =>
		{
			nextCalled = true;
			return Task.CompletedTask;
		});

		var app = builder.Build();

		var context = new DefaultHttpContext();
		context.Request.Path = "/test";
		context.Request.Headers["X-Api-Key"] = apiKey;

		await app(context);

		Assert.True(nextCalled);
	}

	[Fact]
	public async Task UseApiKey_AddsMiddleware_And_BlocksInvalidKey()
	{
		var apiKey = _fixture.Create<string>();
		var builder = CreateBuilderWithConfig(apiKey);

		builder.UseApiKey();
		builder.Run(_ => Task.CompletedTask);

		var app = builder.Build();

		var context = new DefaultHttpContext();
		context.Request.Path = "/test";
		context.Request.Headers["X-Api-Key"] = _fixture.Create<string>(); // wrong key

		await app(context);

		Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
	}
}
