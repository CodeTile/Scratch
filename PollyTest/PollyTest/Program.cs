using Microsoft.FluentUI.AspNetCore.Components;

using PollyTest.Components;
using PollyTest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();


var rootAPI = builder.Configuration["API:root"] ?? throw new NullReferenceException("appsetting.json : API:root");
builder.Services.AddHttpClient("MyApi", client =>
{
	client.BaseAddress = new Uri(rootAPI);
	client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<IWeatherApiService, WeatherApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
