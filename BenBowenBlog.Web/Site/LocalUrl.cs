using System.Diagnostics.CodeAnalysis;

namespace BenBowenBlog.Web.Site;

public static class LocalUrl {
	// Same rules as ASP.NET's IsLocalUrl: a path on this site, never "//evil.example" or "/\evil.example", and no control
	// characters, because browsers strip tabs and newlines (so "/\t/evil.example" would become "//evil.example").
	public static bool IsLocal([NotNullWhen(true)] string? url) {
		if (String.IsNullOrEmpty(url) || url[0] != '/') {
			return false;
		}

		if (url.Length == 1) {
			return true;
		}

		return url[1] != '/' && url[1] != '\\' && !url.AsSpan(1).ContainsAnyInRange('\u0000', '\u001F') && !url.Contains('\u007F');
	}
}
