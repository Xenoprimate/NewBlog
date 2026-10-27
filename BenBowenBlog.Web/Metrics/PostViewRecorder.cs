using System.Security.Cryptography;
using System.Threading.Channels;
using BenBowenBlog.Web.Theming;
using Microsoft.Extensions.Options;

namespace BenBowenBlog.Web.Metrics;

public sealed class MetricsOptions {
	// Secret key for the daily visitor hash (Metrics:Key / Metrics__Key).
	public string? Key { get; set; }
}

public sealed record PendingVisit(int PostId, DateOnly Date, byte[] VisitorHash, string Source);

// Decides whether a post page request counts, then queues it for PostViewWriter so rendering never waits on the database.
public sealed class PostViewRecorder {
	const int QueueCapacity = 10_000;

	readonly byte[] key;
	readonly Channel<PendingVisit> queue = Channel.CreateBounded<PendingVisit>(
		new BoundedChannelOptions(QueueCapacity) {
			FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true
		});

	public PostViewRecorder(IOptions<MetricsOptions> options, ILogger<PostViewRecorder> logger) {
		if (String.IsNullOrWhiteSpace(options.Value.Key)) {
			logger.LogWarning("Metrics:Key isn't configured; using a random key, so visitor de-duplication resets on restart.");
			key = RandomNumberGenerator.GetBytes(32);
		}
		else {
			key = System.Text.Encoding.UTF8.GetBytes(options.Value.Key);
		}
	}

	public ChannelReader<PendingVisit> Reader => queue.Reader;

	public void Record(HttpContext context, int postId) {
		var request = context.Request;
		var userAgent = request.Headers.UserAgent.ToString();

		if (!HttpMethods.IsGet(request.Method)
			|| IsPrefetch(request)
			|| VisitClassifier.IsBot(userAgent)
			|| context.User.Identity?.IsAuthenticated == true
			|| IsChromiumHintRetryPending(request, userAgent)) {
			return;
		}

		var date = DateOnly.FromDateTime(DateTime.UtcNow);
		var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "";
		var campaign = request.Query["ref"].FirstOrDefault() ?? request.Query["utm_source"].FirstOrDefault();
		var source = VisitClassifier.SourceOf(campaign, request.Headers.Referer.ToString(), request.Host.Host);

		// A full queue drops the visit rather than slowing the page down.
		queue.Writer.TryWrite(new PendingVisit(postId, date, VisitClassifier.VisitorHash(key, date, ipAddress, userAgent), source));
	}

	static bool IsPrefetch(HttpRequest request) =>
		request.Headers["Sec-Purpose"].ToString().Contains("prefetch", StringComparison.OrdinalIgnoreCase)
		|| request.Headers["Purpose"].ToString().Contains("prefetch", StringComparison.OrdinalIgnoreCase);

	// On a first visit Chromium browsers request the page without the colour-scheme client hint, then immediately retry
	// with it (Critical-CH, see ThemeEndpoints). Only the retry is counted, so first visits aren't counted twice.
	static bool IsChromiumHintRetryPending(HttpRequest request, string userAgent) =>
		userAgent.Contains("Chrome/", StringComparison.Ordinal)
		&& request.IsHttps
		&& !request.Headers.ContainsKey(ThemePreferences.ColorSchemeHint);
}