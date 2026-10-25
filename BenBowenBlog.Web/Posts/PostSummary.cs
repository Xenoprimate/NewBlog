using BenBowenBlog.Data;

namespace BenBowenBlog.Web.Posts;

public sealed record PostSummary(
    string Slug,
    string Title,
    string? Summary,
    DateTimeOffset PublishedAt,
    string? CoverImage,
    string? CoverImageAlt,
    string CategoryName,
    string CategoryIcon,
    List<string> Tags);

public static class PostQueries
{
    public static IQueryable<Post> Published(this IQueryable<Post> posts) =>
        posts.Where(p => p.PublishedAt != null);

    // Newest first.
    public static IQueryable<PostSummary> SelectSummaries(this IQueryable<Post> posts) =>
        posts
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => new PostSummary(p.Slug, p.Title, p.Summary, p.PublishedAt!.Value, p.CoverImage, p.CoverImageAlt,
                p.Category!.Name, p.Category.Icon, p.Tags));
}

public static class CategoryUrls
{
    // Categories have no stored slug: the URL is derived from the name, so renaming a category changes its URL.
    public static string SlugFor(string categoryName) => Admin.Slugs.FromTitle(categoryName);

    public static string For(string categoryName) => $"/categories/{SlugFor(categoryName)}";
}

public static class TagUrls
{
    public static string For(string tag) => $"/tags/{Uri.EscapeDataString(tag)}";
}
