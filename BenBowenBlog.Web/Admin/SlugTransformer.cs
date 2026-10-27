using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BenBowenBlog.Data;

namespace BenBowenBlog.Web.Admin;

public static partial class SlugTransformer {
	public const string Pattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

	public static string FromTitle(string title) {
		var slug = NonAlphanumericRuns().Replace(RemoveDiacritics(title).ToLowerInvariant(), "-").Trim('-');

		if (slug.Length > Post.SlugMaxLength) {
			slug = slug[..Post.SlugMaxLength].TrimEnd('-');
		}

		return slug;
	}

	// "Café" -> "Cafe": decompose accented characters, then drop the combining marks.
	static string RemoveDiacritics(string text) {
		var builder = new StringBuilder(text.Length);

		foreach (var c in text.Normalize(NormalizationForm.FormD)) {
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) {
				builder.Append(c);
			}
		}

		return builder.ToString().Normalize(NormalizationForm.FormC);
	}

	[GeneratedRegex("[^a-z0-9]+")]
	private static partial Regex NonAlphanumericRuns();
}