using System.Text.RegularExpressions;
using BenBowenBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace BenBowenBlog.Web.Media;

public enum MediaUseKind {
	Cover,
	Inline
}

public sealed record MediaUse(int PostId, string PostTitle, MediaUseKind Kind);

public static partial class MediaUsageUtils {
	// Which posts use each image, as a cover or inline in the body (any /media/{name} reference). Drafts count too.
	// A blog has few enough posts to scan them all.
	public static async Task<Dictionary<string, List<MediaUse>>> FindAsync(BlogDbContext db, CancellationToken cancellationToken = default) {
		var posts = await db.Posts
			.AsNoTracking()
			.Select(p => new {
				p.Id, p.Title, p.CoverImage, p.Body
			})
			.ToListAsync(cancellationToken);

		var uses = new Dictionary<string, List<MediaUse>>();

		foreach (var post in posts) {
			if (post.CoverImage is not null) {
				Add(post.CoverImage, new MediaUse(post.Id, post.Title, MediaUseKind.Cover));
			}

			foreach (var name in InlineReference().Matches(post.Body).Select(match => match.Groups[1].Value).Distinct()) {
				Add(name, new MediaUse(post.Id, post.Title, MediaUseKind.Inline));
			}
		}

		return uses;

		void Add(string name, MediaUse use) {
			if (!uses.TryGetValue(name, out var list)) {
				uses[name] = list = [];
			}

			list.Add(use);
		}
	}

	[GeneratedRegex(@"/media/([0-9a-f]{32}\.(?:jpg|png|gif|webp))")]
	private static partial Regex InlineReference();
}