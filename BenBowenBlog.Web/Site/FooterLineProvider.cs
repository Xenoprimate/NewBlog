using BenBowenBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace BenBowenBlog.Web.Site;

// Picks the random line shown under the footer's copyright notice. The footer renders on every page, so the lines are
// held in memory and reloaded every few minutes, or straight away after the admin page calls Invalidate().
public sealed class FooterLineProvider(IServiceScopeFactory scopeFactory, ILogger<FooterLineProvider> logger) {
	static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

	sealed record Snapshot(string[] Lines, DateTimeOffset LoadedAt, int Version);

	Snapshot? _snapshot;
	int _version;

	public async Task<string?> PickAsync() {
		var lines = await GetLinesAsync();
		return lines.Length == 0 ? null : lines[Random.Shared.Next(lines.Length)];
	}

	public void Invalidate() => Interlocked.Increment(ref _version);

	async Task<string[]> GetLinesAsync() {
		var snapshot = Volatile.Read(ref _snapshot);
		var version = Volatile.Read(ref _version);

		if (snapshot is not null && snapshot.Version == version && DateTimeOffset.UtcNow - snapshot.LoadedAt < RefreshInterval) {
			return snapshot.Lines;
		}

		// A failed load must not break every page, so the footer just goes without its line until the next attempt.
		try {
			await using var scope = scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
			var lines = await db.FooterLines.AsNoTracking().Select(l => l.Text).ToArrayAsync();

			// Stamped with the version read before loading, so a change made mid-load still forces another reload.
			Volatile.Write(ref _snapshot, new Snapshot(lines, DateTimeOffset.UtcNow, version));
			return lines;
		}
		catch (Exception exception) {
			logger.LogWarning(exception, "Couldn't load footer lines");
			return snapshot?.Lines ?? [];
		}
	}
}
