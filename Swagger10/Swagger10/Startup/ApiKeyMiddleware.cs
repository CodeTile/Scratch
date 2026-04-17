namespace Swagger10.Startup
{
	/// <summary>
	/// Global API‑key middleware that protects all endpoints except Swagger/OpenAPI.
	/// Supports optional bypass in local development.
	/// </summary>
	public class ApiKeyMiddleware
	{
		private readonly RequestDelegate _next;

		// The name of the header clients must send
		private const string ApiKeyHeader = "X-Api-Key";

		// The expected API key loaded from configuration (appsettings.json, environment variables, etc.)
		private readonly string _expectedKey;

		public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
		{
			_next = next;

			// Load the API key from configuration.
			// If missing, fail fast so the app doesn't run insecurely.
			_expectedKey = config["ApiKey"] ?? throw new Exception("ApiKey missing");
		}

		public async Task InvokeAsync(HttpContext context)
		{
			// ---------------------------------------------------------
			// Allow Swagger/OpenAPI endpoints without API key
			// ---------------------------------------------------------
			// Swagger UI and OpenAPI JSON must load before authentication,
			// otherwise the UI breaks with 401 errors.
			var path = context.Request.Path.Value;
			if (path!.StartsWith("/openapi") || path.StartsWith("/swagger"))
			{
				await _next(context);
				return;
			}

			// ---------------------------------------------------------
			// Bypass for local development.
			// This is needed as the code to enable you to enter the not
			// yet in place in version 10.
			// ---------------------------------------------------------
			// If running on localhost AND the environment variable
			// BypassApiKeyInDevelopment=true is set,
			// then skip API key validation entirely.
			if (context.Request.Host.Value!.StartsWith("localhost:7107", StringComparison.InvariantCultureIgnoreCase))
			{
				_ = bool.TryParse(Environment.GetEnvironmentVariable("BypassApiKeyInDevelopment"), out bool bypassInDevelopment);
				if (bypassInDevelopment)
				{
					// Skip API key validation
					await _next(context);
					return;
				}
			}

			// ---------------------------------------------------------
			// Validate API key header
			// ---------------------------------------------------------
			// If the header is missing OR does not match the expected key,
			// return 401 Unauthorized.
			if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey) ||
				providedKey != _expectedKey)
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				await context.Response.WriteAsync("Invalid or missing API key");
				return;
			}

			// ---------------------------------------------------------
			// Continue to next middleware
			// ---------------------------------------------------------
			await _next(context);
		}
	}
}
