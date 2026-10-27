namespace BenBowenBlog.Web.Theming;

public static class ThemeEndpoints {
	static readonly CookieOptions CookieOptions = new() {
		MaxAge = TimeSpan.FromDays(365), HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax,
		IsEssential = true
	};

	// Plain links from the footer's theme picker: set the cookie and go back to the page the reader was on.
	public static IEndpointRouteBuilder MapThemeEndpoints(this IEndpointRouteBuilder endpoints) {
		endpoints.MapGet("/theme/palette/{key}", (string key, string? returnUrl, HttpContext context) => {
			if (BlogThemes.Contains(key)) {
				context.Response.Cookies.Append(ThemePreferences.PaletteCookie, key, CookieOptions);
			}

			return Results.LocalRedirect(SafeReturnUrl(returnUrl));
		});

		endpoints.MapGet("/theme/mode/{mode}", (string mode, string? returnUrl, HttpContext context) => {
			if (Enum.TryParse<ThemeMode>(mode, true, out var parsed)) {
				context.Response.Cookies.Append(ThemePreferences.ModeCookie, parsed.ToString().ToLowerInvariant(), CookieOptions);
			}

			return Results.LocalRedirect(SafeReturnUrl(returnUrl));
		});

		endpoints.MapGet("/theme/pattern/{pattern}", (string pattern, string? returnUrl, HttpContext context) => {
			if (Enum.TryParse<BackgroundPattern>(pattern, true, out var parsed)) {
				context.Response.Cookies.Append(ThemePreferences.PatternCookie, parsed.ToString().ToLowerInvariant(), CookieOptions);
			}

			return Results.LocalRedirect(SafeReturnUrl(returnUrl));
		});

		return endpoints;
	}

	// Asks Chromium browsers to send their light/dark preference with page requests. Critical-CH makes the very first
	// request retry once with the hint, so "System" is right from the first page view; Vary keeps caches honest.
	public static IApplicationBuilder UseColorSchemeClientHint(this IApplicationBuilder app) =>
		app.Use(async (context, next) => {
			if (context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)) {
				context.Response.Headers["Accept-CH"] = ThemePreferences.ColorSchemeHint;
				context.Response.Headers["Critical-CH"] = ThemePreferences.ColorSchemeHint;
				context.Response.Headers.Append("Vary", ThemePreferences.ColorSchemeHint);
			}

			await next();
		});

	// Only paths on this site, never "//evil.example" or "/\evil.example".
	static string SafeReturnUrl(string? url) =>
		url is {Length: > 0} && url[0] == '/' && (url.Length == 1 || url[1] != '/' && url[1] != '\\') ? url : "/";
}