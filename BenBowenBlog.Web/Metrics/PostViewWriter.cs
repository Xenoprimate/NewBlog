using BenBowenBlog.Data;
using Microsoft.EntityFrameworkCore;

namespace BenBowenBlog.Web.Metrics;

// Drains PostViewRecorder's queue into PostVisits: the first view of a post by a visitor on a day adds a row, later ones
// that day increment its view count.
public sealed class PostViewWriter(PostViewRecorder recorder, IServiceScopeFactory scopeFactory, ILogger<PostViewWriter> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var visit in recorder.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

                await db.Database.ExecuteSqlAsync($"""
                    INSERT INTO "PostVisits" ("PostId", "Date", "VisitorHash", "Views", "Source")
                    VALUES ({visit.PostId}, {visit.Date}, {visit.VisitorHash}, 1, {visit.Source})
                    ON CONFLICT ("PostId", "Date", "VisitorHash") DO UPDATE SET "Views" = "PostVisits"."Views" + 1
                    """, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                // E.g. the post was deleted in the meantime; metrics must never take the site down.
                logger.LogWarning(e, "Couldn't record a visit to post {PostId}", visit.PostId);
            }
        }
    }
}
