using System.ComponentModel.DataAnnotations;
using BenBowenBlog.Data;

namespace BenBowenBlog.Web.Admin;

// Form model for the post editor. Only these fields are copied onto a Post, so the form can't set anything else
// (Id, CreatedAt, ...). The cover image's blob name is managed by the editor page, not the form.
public sealed class PostEditModel : IValidatableObject
{
    [Required]
    [MaxLength(PostLimits.SlugMaxLength)]
    [RegularExpression(Slugs.Pattern, ErrorMessage = "Use lowercase letters, digits and single hyphens only.")]
    public string Slug { get; set; } = "";

    [Required]
    [MaxLength(PostLimits.TitleMaxLength)]
    public string Title { get; set; } = "";

    [MaxLength(PostLimits.SummaryMaxLength)]
    public string? Summary { get; set; }

    [Required]
    public string Body { get; set; } = "";

    [Required(ErrorMessage = "Choose a category.")]
    public int? CategoryId { get; set; }

    public string TagsText { get; set; } = "";

    [MaxLength(PostLimits.CoverImageAltMaxLength)]
    public string? CoverImageAlt { get; set; }

    public bool IsPublished { get; set; }

    // The post this one follows in a series (see Post.PreviousPostId).
    public int? PreviousPostId { get; set; }

    public static PostEditModel FromPost(Post post) => new()
    {
        Slug = post.Slug,
        Title = post.Title,
        Summary = post.Summary,
        Body = post.Body,
        CategoryId = post.CategoryId,
        TagsText = string.Join(", ", post.Tags),
        CoverImageAlt = post.CoverImageAlt,
        IsPublished = post.PublishedAt is not null,
        PreviousPostId = post.PreviousPostId
    };

    public Post CreatePost(DateTimeOffset utcNow)
    {
        var post = new Post { Slug = Slug, Title = Title, Body = Body, CreatedAt = utcNow };
        ApplyTo(post, utcNow);
        return post;
    }

    public void ApplyTo(Post post, DateTimeOffset utcNow)
    {
        post.Slug = Slug;
        post.Title = Title;
        post.Summary = string.IsNullOrWhiteSpace(Summary) ? null : Summary;
        post.Body = Body;
        post.CategoryId = CategoryId!.Value;
        post.Tags = Tags.Normalize(TagsText);
        post.CoverImageAlt = string.IsNullOrWhiteSpace(CoverImageAlt) ? null : CoverImageAlt;
        post.PreviousPostId = PreviousPostId;

        // Keep the original publish date when re-saving a published post.
        post.PublishedAt = IsPublished ? post.PublishedAt ?? utcNow : null;
        post.UpdatedAt = utcNow;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var tags = Tags.Normalize(TagsText);

        if (tags.Count > PostLimits.MaxTags)
        {
            yield return new($"Use at most {PostLimits.MaxTags} tags.", [nameof(TagsText)]);
        }

        if (tags.Any(tag => tag.Length > PostLimits.TagMaxLength))
        {
            yield return new($"Each tag must be at most {PostLimits.TagMaxLength} characters.", [nameof(TagsText)]);
        }
    }
}
