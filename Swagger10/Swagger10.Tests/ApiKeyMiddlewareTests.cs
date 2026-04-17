namespace Swagger10.Tests;

using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using Swagger10.Startup;

using Xunit;

public class ApiKeyMiddlewareTests
{
	private const string ValidApiKey = "test-key";

	public ApiKeyMiddlewareTests()
	{

	}

	private ApiKeyMiddleware CreateMiddleware(RequestDelegate next)
	{
		var inMemorySettings = new Dictionary<string, string?>
		{
			{ "ApiKey", ValidApiKey }
		};

		IConfiguration config = new ConfigurationBuilder()
			.AddInMemoryCollection(inMemorySettings)
			.Build();

		return new ApiKeyMiddleware(next, config);
	}

	private HttpContext CreateContext(string path = "/", string? host = "localhost:7107")
	{
		var context = new DefaultHttpContext();
		context.Request.Path = path;
		context.Request.Host = new HostString(host);
		return context;
	}

	[Fact]
	public async Task Allows_Swagger_Endpoints_Without_ApiKey()
	{
		bool nextCalled = false;
		var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

		var context = CreateContext("/swagger/index.html");

		await middleware.InvokeAsync(context);

		Assert.True(nextCalled);
	}

	[Fact]
	public async Task Allows_OpenApi_Endpoints_Without_ApiKey()
	{
		bool nextCalled = false;
		var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

		var context = CreateContext("/openapi/v1.json");

		await middleware.InvokeAsync(context);

		Assert.True(nextCalled);
	}

	[Fact]
	public async Task Bypasses_ApiKey_When_Environment_Variable_Is_True()
	{
		Environment.SetEnvironmentVariable("BypassApiKeyInDevelopment", "true");

		bool nextCalled = false;
		var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

		var context = CreateContext("/weather");

		await middleware.InvokeAsync(context);

		Assert.True(nextCalled);

		Environment.SetEnvironmentVariable("BypassApiKeyInDevelopment", null);
	}

	[Fact]
	public async Task Returns_401_When_ApiKey_Is_Missing()
	{
		var middleware = CreateMiddleware(_ => Task.CompletedTask);

		var context = CreateContext("/weather");

		await middleware.InvokeAsync(context);

		Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
	}

	[Fact]
	public async Task Returns_401_When_ApiKey_Is_Invalid()
	{
		var middleware = CreateMiddleware(_ => Task.CompletedTask);

		var context = CreateContext("/weather");
		context.Request.Headers["X-Api-Key"] = "wrong-key";

		await middleware.InvokeAsync(context);

		Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
	}

	[Fact]
	public async Task Calls_Next_When_ApiKey_Is_Valid()
	{
		bool nextCalled = false;
		var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

		var context = CreateContext("/weather");
		context.Request.Headers["X-Api-Key"] = ValidApiKey;

		await middleware.InvokeAsync(context);

		Assert.True(nextCalled);
	}
}

