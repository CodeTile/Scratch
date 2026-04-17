namespace Swagger10
{
	public class ApiKeyMiddleware
	{
		private readonly RequestDelegate _next;
		private const string ApiKeyHeader = "X-Api-Key";
		private readonly string _expectedKey;

		public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
		{
			_next = next;
			_expectedKey = config["ApiKey"] ?? throw new Exception("ApiKey missing");
		}

		public async Task InvokeAsync(HttpContext context)
		{
			// Allow Swagger/OpenAPI without API key
			var path = context.Request.Path.Value;
			if (path.StartsWith("/openapi") || path.StartsWith("/swagger"))
			{
				await _next(context);
				return;
			}

			if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey) ||
				providedKey != _expectedKey)
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsync("Invalid or missing API key");
				return;
			}

			await _next(context);
		}

	}

	public static class ApiKeyMiddlewareExtensions
	{
		public static IApplicationBuilder UseApiKey(this IApplicationBuilder app)
		{
			return app.UseMiddleware<ApiKeyMiddleware>();
		}
	}

}
