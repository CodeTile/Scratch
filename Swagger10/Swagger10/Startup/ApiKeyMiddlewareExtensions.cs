namespace Swagger10.Startup;

public static class ApiKeyMiddlewareExtensions
{
	public static IApplicationBuilder UseApiKey(this IApplicationBuilder app)
	{
		return app.UseMiddleware<ApiKeyMiddleware>();
	}
}