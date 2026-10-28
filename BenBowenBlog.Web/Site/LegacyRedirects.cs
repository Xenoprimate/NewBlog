using BenBowenBlog.Data;
using BenBowenBlog.Web.Posts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BenBowenBlog.Web.Site;

// Permanent (301) redirects so links to the old site keep working: www to the bare domain, the old feed, old post URLs
// (/post/some_post/ -> /post/some-post, when that post is published here) and trailing slashes.
public static class LegacyRedirects {
	const string PostPrefix = "/post/";

	public static IApplicationBuilder UseLegacyRedirects(this IApplicationBuilder app) =>
		app.Use(async (context, next) => {
			if (await TargetFor(context) is { } target) {
				context.Response.Redirect(target, permanent: true);
				return;
			}

			await next();
		});

	static async Task<string?> TargetFor(HttpContext context) {
		var request = context.Request;
		if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) {
			return null;
		}

		var path = request.Path.Value ?? "/";
		var query = request.QueryString.Value ?? "";

		var canonicalHost = context.RequestServices.GetRequiredService<IOptions<SiteOptions>>().Value.CanonicalHost;
		if (!String.IsNullOrEmpty(canonicalHost) && request.Host.Host.Equals("www." + canonicalHost, StringComparison.OrdinalIgnoreCase)) {
			return $"https://{canonicalHost}{request.PathBase}{path}{query}";
		}

		if (path.Equals("/rss.xml", StringComparison.OrdinalIgnoreCase)) {
			return "/feed.xml";
		}

		// New slugs never contain underscores (SlugTransformer.Pattern), so one here means an old-site URL.
		if (path.StartsWith(PostPrefix, StringComparison.OrdinalIgnoreCase)) {
			var oldSlug = path[PostPrefix.Length..].Split('/')[0];
			if (oldSlug.Contains('_')) {
				var slug = oldSlug.ToLowerInvariant().Replace('_', '-');
				var db = context.RequestServices.GetRequiredService<BlogDbContext>();
				var published = await db.Posts.AsNoTracking().Published().AnyAsync(p => p.Slug == slug, context.RequestAborted);
				return published ? $"{PostPrefix}{slug}{query}" : null;
			}
		}

		// Framework paths (/_blazor, /_framework, ...) are left alone.
		if (path.Length > 1 && path.EndsWith('/') && !path.StartsWith("/_")) {
			return path.TrimEnd('/') + query;
		}

		return null;
	}
}
