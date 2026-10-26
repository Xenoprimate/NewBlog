using System.Text.RegularExpressions;

namespace BenBowenBlog.Web.Media;

public static partial class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/media/{name}", async (string name, MediaStore store, HttpContext context) =>
        {
            if (!BlobName().IsMatch(name))
            {
                return Results.NotFound();
            }

            var blob = await store.OpenAsync(name, context.RequestAborted);

            if (blob is null)
            {
                return Results.NotFound();
            }

            // Names are random and never reused, so the content behind a URL never changes.
            context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            context.Response.Headers.XContentTypeOptions = "nosniff";

            return Results.Stream(blob.Content, blob.Details.ContentType, enableRangeProcessing: false);
        });

        return endpoints;
    }

    [GeneratedRegex("^[0-9a-f]{32}\\.(jpg|png|gif|webp)$")]
    private static partial Regex BlobName();
}
