using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BenBowenBlog.Data;

namespace BenBowenBlog.Web.Metrics;

// Pure helpers for deciding whether a request counts as a visit, where it came from, and who (anonymously) made it.
public static partial class VisitClassifier {
	public const string Direct = "(direct)";
	public const string Internal = "(internal)";

	// Campaign tags are stored with this prefix so they can be told apart from referring hosts.
	public const string CampaignPrefix = "ref:";

	const int CampaignMaxLength = 50;

	public static bool IsBot(string? userAgent) => String.IsNullOrWhiteSpace(userAgent) || BotPattern().IsMatch(userAgent);

	// ?ref= / ?utm_source= wins; otherwise the referring host ("www." dropped), "(internal)" for this site, or "(direct)".
	public static string SourceOf(string? campaign, string? referer, string ownHost) {
		if (!String.IsNullOrWhiteSpace(campaign)) {
			var tag = campaign.Trim().ToLowerInvariant();
			return CampaignPrefix + (tag.Length > CampaignMaxLength ? tag[..CampaignMaxLength] : tag);
		}

		if (String.IsNullOrWhiteSpace(referer) || !Uri.TryCreate(referer, UriKind.Absolute, out var uri) || String.IsNullOrEmpty(uri.Host)) {
			return Direct;
		}

		if (String.Equals(uri.Host, ownHost, StringComparison.OrdinalIgnoreCase)) {
			return Internal;
		}

		var host = uri.Host.ToLowerInvariant();
		host = host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;

		return host.Length > PostVisit.SourceMaxLength ? host[..PostVisit.SourceMaxLength] : host;
	}

	// Keyed and dated, so it can't be reversed to an IP address without the key, and the same reader gets a different
	// hash every day.
	public static byte[] VisitorHash(byte[] key, DateOnly date, string ipAddress, string userAgent) {
		var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{date:yyyy-MM-dd}|{ipAddress}|{userAgent}"));
		return hash[..PostVisit.VisitorHashLength];
	}

	[GeneratedRegex(@"bot|crawl|spider|slurp|preview|monitor|facebookexternalhit|embedly|curl|wget|python|httpclient|okhttp|java/|go-http|headless|lighthouse|pingdom|uptime|feed|rss|scrapy|axios|node-fetch|postman",
		RegexOptions.IgnoreCase)]
	private static partial Regex BotPattern();
}