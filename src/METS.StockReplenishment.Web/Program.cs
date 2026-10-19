using METS.StockReplenishment.Core.Interfaces;
using METS.StockReplenishment.Infrastructure.Data;
using METS.StockReplenishment.Infrastructure.Repositories;
using METS.StockReplenishment.Infrastructure.Services;
using METS.StockReplenishment.Web.Components;
using METS.StockReplenishment.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// SQLite in-memory: keep a single connection open for the app lifetime
var connection = new SqliteConnection("DataSource=METSStockReplenishment;Mode=Memory;Cache=Shared");
connection.Open();
builder.Services.AddSingleton(connection);

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlite(connection));

// Identity configuration
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
	options.Password.RequiredLength = 6;
	options.Password.RequireDigit = false;
	options.Password.RequireUppercase = false;
	options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/login";
	options.LogoutPath = "/logout";
	options.SlidingExpiration = true;
	options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// HttpContextAccessor for reading current user in AuthenticationStateProvider
builder.Services.AddHttpContextAccessor();

// Custom AuthenticationStateProvider for Blazor Server
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// Authentication Service
builder.Services.AddTransient<AuthCookieForwardingHandler>();

// Repositories & Services
builder.Services.AddScoped<IReplenishmentRequestRepository, ReplenishmentRequestRepository>();
builder.Services.AddScoped<IReplenishmentRequestService, ReplenishmentRequestService>();
builder.Services.AddScoped<IStockValidationService, StockValidationService>();

// Background service for async stock validation
builder.Services.AddSingleton<StockValidationBackgroundService>();
builder.Services.AddSingleton<IStockValidationQueue>(sp => sp.GetRequiredService<StockValidationBackgroundService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<StockValidationBackgroundService>());

// API Controllers
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Converters.Add(
			new System.Text.Json.Serialization.JsonStringEnumConverter());
	});

// MudBlazor
builder.Services.AddMudServices();

// Blazor Server
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

// HttpClient for Blazor components to call the local API.
// Keep resolution scoped so NavigationManager is available, and forward auth cookie.
builder.Services.AddScoped(sp =>
{
	var nav = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
	var handler = sp.GetRequiredService<AuthCookieForwardingHandler>();
	handler.InnerHandler = new HttpClientHandler();

	return new HttpClient(handler)
	{
		BaseAddress = new Uri(nav.BaseUri)
	};
});

var app = builder.Build();

// Create database schema and seed data
using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	context.Database.EnsureCreated();
	DataSeeder.Seed(context);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();