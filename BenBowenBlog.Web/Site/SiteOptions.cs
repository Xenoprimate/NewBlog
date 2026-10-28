namespace BenBowenBlog.Web.Site;

// Bound from the "Site" configuration section. A social link with no URL is left out of the menu.
public sealed class SiteOptions {
	public string? GitHubUrl { get; set; }

	public string? LinkedInUrl { get; set; }

	public string? BlueskyUrl { get; set; }

	// The site's own host name (e.g. "benbowen.blog"); requests for www.<CanonicalHost> are redirected to it.
	public string? CanonicalHost { get; set; }
}