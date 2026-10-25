namespace BenBowenBlog.Web.Site;

// Bound from the "Site" configuration section. A social link with no URL is left out of the menu.
public sealed class SiteOptions
{
    public string? GitHubUrl { get; set; }

    public string? LinkedInUrl { get; set; }

    public string? BlueskyUrl { get; set; }
}
