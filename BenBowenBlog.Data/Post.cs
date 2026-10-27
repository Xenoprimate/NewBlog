namespace BenBowenBlog.Data;

public class Post {
	public const int SlugMaxLength = 200;
	public const int TitleMaxLength = 300;
	public const int SummaryMaxLength = 1000;

	public const int MaxTags = 10;
	public const int TagMaxLength = 50;

	public const int CoverImageNameMaxLength = 100;
	public const int CoverImageAltMaxLength = 300;
	public const long CoverImageMaxBytes = 10 * 1024 * 1024;

	public int Id { get; set; }

	public required string Slug { get; set; }

	public required string Title { get; set; }

	public string? Summary { get; set; }

	public required string Body { get; set; }

	public int CategoryId { get; set; }

	public Category? Category { get; set; }

	public List<string> Tags { get; set; } = [];

	// Blob name in the "covers" container, e.g. "3f2a...c9.png".
	public string? CoverImage { get; set; }

	public string? CoverImageAlt { get; set; }

	// The post this one follows in a series. The next post is whichever post points back at this one.
	public int? PreviousPostId { get; set; }

	public Post? PreviousPost { get; set; }

	public List<Erratum> Errata { get; set; } = [];

	public DateTimeOffset? PublishedAt { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset UpdatedAt { get; set; }
}