namespace BenBowenBlog.Web.Site;

public static class SecurityHeaders {
	// No script-src: Blazor, MudBlazor and the raw HTML allowed in posts would all need nonces. These cover framing
	// (clickjacking), <base> and plugin injection, and where forms may post.
	const string ContentSecurityPolicy = "frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self'";

	// Added as the response starts, so error and not-found responses get them too (the exception handler clears headers).
	public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
		app.Use(async (context, next) => {
			context.Response.OnStarting(() => {
				var headers = context.Response.Headers;
				headers.XFrameOptions = "DENY";
				headers.ContentSecurityPolicy = ContentSecurityPolicy;
				headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
				headers.XContentTypeOptions = "nosniff";
				return Task.CompletedTask;
			});

			await next();
		});
}
