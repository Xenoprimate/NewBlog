using BenBowenBlog.Data;
using BenBowenBlog.Web.Admin;
using BenBowenBlog.Web.Media;
using BenBowenBlog.Web.Metrics;
using BenBowenBlog.Web.Site;
using BenBowenBlog.Web.Theming;
using BenBowenBlog.Web.Components;
using BenBowenBlog.Web.Rss;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddAzureNpgsqlDbContext<BlogDbContext>("blogdb");
builder.AddAzureBlobServiceClient("blobs");

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents()
	// The post editor sends the whole Markdown body in one message; the 32 KB default silently drops long posts.
	.AddHubOptions(options => options.MaximumReceiveMessageSize = 1024 * 1024);

builder.Services.AddMudServices();

builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection("Site"));

// Admin pages are unlocked with a private key (Admin:Key) at /admin/unlock, which issues this cookie.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options => {
		options.Cookie.Name = "blog-admin";
		options.Cookie.HttpOnly = true;
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
		options.Cookie.SameSite = SameSiteMode.Strict;
		options.ExpireTimeSpan = TimeSpan.FromHours(8);
		options.SlidingExpiration = true;
		options.LoginPath = "/admin/unlock";
	});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<AdminKeyAuthenticator>();
builder.Services.AddSingleton<IconCatalog>();
builder.Services.AddSingleton<MediaStore>();
builder.Services.AddSingleton<FooterLineProvider>();

// Private post metrics: post pages queue visits, a background service writes them (see Metrics/).
builder.Services.Configure<MetricsOptions>(builder.Configuration.GetSection("Metrics"));
builder.Services.AddSingleton<PostViewRecorder>();
builder.Services.AddHostedService<PostViewWriter>();

builder.Services.AddRequestTimeouts();

builder.Services.AddOutputCache();

var app = builder.Build();

app.UseSecurityHeaders();

// Apply any pending EF Core migrations before serving requests. Fine for a single instance; with several
// instances starting at once, run migrations as a separate deployment step instead.
await using (var scope = app.Services.CreateAsyncScope()) {
	var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
	await db.Database.CreateExecutionStrategy().ExecuteAsync(() => db.Database.MigrateAsync());
}

await app.Services.GetRequiredService<MediaStore>().EnsureContainerAsync();

if (!app.Environment.IsDevelopment()) {
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

// Requests that match no page (and other bodiless error responses) render the Blazor not-found page.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

// Old benbowen.blog URLs (and www.) are redirected permanently to their new equivalents.
app.UseLegacyRedirects();

app.UseColorSchemeClientHint();

// Keep admin pages out of search engines.
app.Use(async (context, next) => {
	if (context.Request.Path.StartsWithSegments("/admin")) {
		context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
	}

	await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseRequestTimeouts();

app.UseOutputCache();

app.MapStaticAssets();

app.MapMediaEndpoints();
app.MapRssEndpoints();
app.MapThemeEndpoints();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();