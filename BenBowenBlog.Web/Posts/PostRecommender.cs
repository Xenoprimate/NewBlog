using BenBowenBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace BenBowenBlog.Web.Posts;

public sealed record RecommendedPost(string Slug, string Title, DateTimeOffset PublishedAt, string? CoverImage);

public sealed record PostRecommendationSet(RecommendedPost? Previous, RecommendedPost? Next, IReadOnlyList<RecommendedPost> Recommended)
{
    public bool IsEmpty => Previous is null && Next is null && Recommended.Count == 0;
}

public static class PostRecommender
{
    public const int RecommendedCount = 4;

    private const int SameCategoryScore = 3;
    private const int SharedTagScore = 1;

    // The series chain (previous/next) first, then the best matches by category and shared tags, newest first on ties.
    // If too few posts match, the list is topped up with the newest posts so it's never short.
    public static async Task<PostRecommendationSet> GetAsync(BlogDbContext db, Post post, CancellationToken cancellationToken = default)
    {
        // A blog has few enough posts to score them all in memory.
        var candidates = await db.Posts
            .AsNoTracking()
            .Published()
            .Where(p => p.Id != post.Id)
            .Select(p => new Candidate(p.Id, p.Slug, p.Title, p.PublishedAt!.Value, p.CoverImage, p.CategoryId, p.Tags, p.PreviousPostId))
            .ToListAsync(cancellationToken);

        var previous = post.PreviousPostId is { } previousId ? candidates.FirstOrDefault(c => c.Id == previousId) : null;
        var next = candidates.FirstOrDefault(c => c.PreviousPostId == post.Id);
        var tags = post.Tags.ToHashSet();

        var others = candidates.Where(c => c != previous && c != next).ToList();

        var recommended = others
            .Select(c => (Candidate: c, Score: (c.CategoryId == post.CategoryId ? SameCategoryScore : 0) + c.Tags.Count(tags.Contains) * SharedTagScore))
            .Where(scored => scored.Score > 0)
            .OrderByDescending(scored => scored.Score)
            .ThenByDescending(scored => scored.Candidate.PublishedAt)
            .Select(scored => scored.Candidate)
            .Take(RecommendedCount)
            .ToList();

        if (recommended.Count < RecommendedCount)
        {
            recommended.AddRange(others
                .Where(c => !recommended.Contains(c))
                .OrderByDescending(c => c.PublishedAt)
                .Take(RecommendedCount - recommended.Count));
        }

        return new PostRecommendationSet(previous?.ToRecommended(), next?.ToRecommended(), recommended.Select(c => c.ToRecommended()).ToList());
    }

    // A class rather than a record: candidates are compared by reference, not by value.
    private sealed class Candidate(int id, string slug, string title, DateTimeOffset publishedAt, string? coverImage, int categoryId, List<string> tags, int? previousPostId)
    {
        public int Id { get; } = id;

        public DateTimeOffset PublishedAt { get; } = publishedAt;

        public int CategoryId { get; } = categoryId;

        public List<string> Tags { get; } = tags;

        public int? PreviousPostId { get; } = previousPostId;

        public RecommendedPost ToRecommended() => new(slug, title, PublishedAt, coverImage);
    }
}
