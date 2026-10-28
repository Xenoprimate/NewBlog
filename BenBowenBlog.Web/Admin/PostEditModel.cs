using System.ComponentModel.DataAnnotations;
using BenBowenBlog.Data;

namespace BenBowenBlog.Web.Admin;

// Form model for the post editor. Only these fields are copied onto a Post, so the form can't set anything else
// (Id, CreatedAt, ...). The cover image's blob name is managed by the editor page, not the form.
public sealed class ErratumEditModel {
	public DateTime? Date { get; set; }

	public string Text { get; set; } = "";
}

public sealed class PostEditModel : IValidatableObject {
	[Required, MaxLength(Post.SlugMaxLength), RegularExpression(SlugTransformer.Pattern, ErrorMessage = "Use lowercase letters, digits and single hyphens only.")]
	public string Slug { get; set; } = "";

	[Required, MaxLength(Post.TitleMaxLength)]
	public string Title { get; set; } = "";

	[MaxLength(Post.SummaryMaxLength)]
	public string? Summary { get; set; }

	[Required]
	public string Body { get; set; } = "";

	[Required(ErrorMessage = "Choose a category.")]
	public int? CategoryId { get; set; }

	public string TagsText { get; set; } = "";

	[MaxLength(Post.CoverImageAltMaxLength)]
	public string? CoverImageAlt { get; set; }

	public bool IsPublished { get; set; }

	// Only the date: empty means "now" when first publishing (see ApplyTo).
	public DateTime? PublishedDate { get; set; }

	// The post this one follows in a series (see Post.PreviousPostId).
	public int? PreviousPostId { get; set; }

	public List<ErratumEditModel> Errata { get; set; } = [];

	public static PostEditModel FromPost(Post post) => new() {
		Slug = post.Slug, Title = post.Title, Summary = post.Summary, Body = post.Body,
		CategoryId = post.CategoryId, TagsText = String.Join(", ", post.Tags), CoverImageAlt = post.CoverImageAlt, IsPublished = post.PublishedAt is not null,
		PublishedDate = post.PublishedAt?.UtcDateTime.Date,
		PreviousPostId = post.PreviousPostId, Errata = post.Errata
			.OrderBy(e => e.Date)
			.Select(e => new ErratumEditModel {
				Date = e.Date.ToDateTime(TimeOnly.MinValue), Text = e.Text
			})
			.ToList()
	};

	public Post CreatePost(DateTimeOffset utcNow) {
		var post = new Post {
			Slug = Slug, Title = Title, Body = Body, CreatedAt = utcNow
		};
		ApplyTo(post, utcNow);
		return post;
	}

	public void ApplyTo(Post post, DateTimeOffset utcNow) {
		post.Slug = Slug;
		post.Title = Title;
		post.Summary = String.IsNullOrWhiteSpace(Summary) ? null : Summary;
		post.Body = Body;
		post.CategoryId = CategoryId!.Value;
		post.Tags = TagTransformer.Normalize(TagsText);
		post.CoverImageAlt = String.IsNullOrWhiteSpace(CoverImageAlt) ? null : CoverImageAlt;
		post.PreviousPostId = PreviousPostId;

		// Replaced wholesale: errata removed here are orphans of a required relationship, so EF deletes them.
		post.Errata.Clear();
		post.Errata.AddRange(Errata.Select(e => new Erratum {
			Date = DateOnly.FromDateTime(e.Date!.Value), Text = e.Text.Trim()
		}));

		post.PublishedAt = IsPublished ? PublishedAtFor(post.PublishedAt, utcNow) : null;
		post.UpdatedAt = utcNow;
	}

	// Re-saving keeps the existing timestamp unless a different date was chosen; a chosen date is stored at 12:00 UTC so
	// no time zone shows it as the day before or after.
	DateTimeOffset PublishedAtFor(DateTimeOffset? existing, DateTimeOffset utcNow) {
		if (PublishedDate is not { } date) {
			return existing ?? utcNow;
		}

		if (existing is { } current && current.UtcDateTime.Date == date.Date) {
			return current;
		}

		return new DateTimeOffset(date.Year, date.Month, date.Day, 12, 0, 0, TimeSpan.Zero);
	}

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
		// Posts with any PublishedAt are listed straight away, so a future date would just show up early, dated ahead.
		if (IsPublished && PublishedDate?.Date > DateTime.UtcNow.Date) {
			yield return new("Publish date can't be in the future.", [nameof(PublishedDate)]);
		}

		var tags = TagTransformer.Normalize(TagsText);

		if (tags.Count > Post.MaxTags) {
			yield return new($"Use at most {Post.MaxTags} tags.", [nameof(TagsText)]);
		}

		if (tags.Any(tag => tag.Length > Post.TagMaxLength)) {
			yield return new($"Each tag must be at most {Post.TagMaxLength} characters.", [nameof(TagsText)]);
		}

		for (var i = 0; i < Errata.Count; i++) {
			var erratum = Errata[i];

			if (erratum.Date is null) {
				yield return new($"Erratum {i + 1} needs a date.", [nameof(Errata)]);
			}

			if (String.IsNullOrWhiteSpace(erratum.Text)) {
				yield return new($"Erratum {i + 1} needs some text.", [nameof(Errata)]);
			}
			else if (erratum.Text.Trim().Length > Erratum.TextMaxLength) {
				yield return new($"Erratum {i + 1} is longer than {Erratum.TextMaxLength} characters.", [nameof(Errata)]);
			}
		}
	}
}