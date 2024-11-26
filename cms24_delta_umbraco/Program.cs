using cms24_delta_umbraco.Contexts;
using cms24_delta_umbraco.Hubs;
using cms24_delta_umbraco.Interfaces;
using cms24_delta_umbraco.Models;
using cms24_delta_umbraco.Services;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowLocalhost3000", policy =>
	{
		policy.WithOrigins("http://localhost:3000")
			  .AllowAnyMethod()
			  .AllowAnyHeader()
			  .AllowCredentials();
	});
});

builder.Services.Configure<SpotifySettings>(builder.Configuration.GetSection("Spotify"));
builder.Services.AddScoped<ISpotifyService, SpotifyService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(15);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});


builder.Services.Configure<CookiePolicyOptions>(options =>
{
	options.MinimumSameSitePolicy = SameSiteMode.Strict;
	options.Secure = CookieSecurePolicy.None;
});

builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddHostedService<RoomCleanupService>();
builder.Services.AddSignalR(options =>
{
	options.KeepAliveInterval = TimeSpan.FromSeconds(10);
	options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
});

builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("umbracoDbDSN")));

builder.CreateUmbracoBuilder()
	.AddBackOffice()
	.AddWebsite()
	.AddDeliveryApi()
	.AddComposers()
	.Build();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.UseSession();

app.UseCors("AllowLocalhost3000");



app.UseUmbraco()
	.WithMiddleware(u =>
	{
		u.UseBackOffice();
		u.UseWebsite();
	})
	.WithEndpoints(u =>
	{
		u.UseBackOfficeEndpoints();
		u.UseWebsiteEndpoints();
	});

app.MapHub<LobbyHub>("/lobbyHub");
app.MapHub<GameHub>("/gameHub");
app.MapControllers();

await app.RunAsync();
