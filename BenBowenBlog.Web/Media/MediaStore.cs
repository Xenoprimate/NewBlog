using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BenBowenBlog.Web.Media;

// All uploaded images (post covers and inline images) live in the private "media" blob container and are served by
// MediaEndpoints at /media/{name}.
public sealed class MediaStore(BlobServiceClient blobServiceClient)
{
    // Blob metadata is sent as HTTP headers, which must be ASCII, so the original file name is stored URL-encoded.
    private const string OriginalNameMetadataKey = "originalname";

    private readonly BlobContainerClient container = blobServiceClient.GetBlobContainerClient("media");

    public Task EnsureContainerAsync(CancellationToken cancellationToken = default) =>
        container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

    // The type is decided from the file's own bytes rather than the browser's claim, and only these four raster
    // formats are accepted (SVG can carry script, so it's deliberately excluded).
    public static ImageFormat? DetectFormat(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
        {
            return new("jpg", "image/jpeg");
        }

        if (data.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return new("png", "image/png");
        }

        if (data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8))
        {
            return new("gif", "image/gif");
        }

        if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8))
        {
            return new("webp", "image/webp");
        }

        return null;
    }

    public async Task<string> UploadAsync(byte[] data, ImageFormat format, string? originalName, CancellationToken cancellationToken = default)
    {
        var name = $"{Guid.NewGuid():N}.{format.Extension}";
        var metadata = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(originalName))
        {
            metadata[OriginalNameMetadataKey] = Uri.EscapeDataString(Path.GetFileName(originalName));
        }

        await container.GetBlobClient(name).UploadAsync(
            new BinaryData(data),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = format.ContentType }, Metadata = metadata },
            cancellationToken);

        return name;
    }

    // Newest first. Blobs uploaded before original names were recorded fall back to their random name for display.
    public async Task<List<MediaItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<MediaItem>();

        await foreach (var blob in container.GetBlobsAsync(BlobTraits.Metadata, BlobStates.None, prefix: null, cancellationToken))
        {
            var originalName = blob.Metadata.TryGetValue(OriginalNameMetadataKey, out var encoded) ? Uri.UnescapeDataString(encoded) : null;
            items.Add(new MediaItem(blob.Name, blob.Properties.ContentType, blob.Properties.ContentLength ?? 0, blob.Properties.CreatedOn, originalName));
        }

        return items.OrderByDescending(item => item.UploadedAt).ToList();
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken = default) =>
        await container.GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public async Task<BlobDownloadStreamingResult?> OpenAsync(string name, CancellationToken cancellationToken = default)
    {
        var blob = container.GetBlobClient(name);

        try
        {
            return (await blob.DownloadStreamingAsync(cancellationToken: cancellationToken)).Value;
        }
        catch (Azure.RequestFailedException e) when (e.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    public static string UrlFor(string name) => $"/media/{name}";
}

public sealed record ImageFormat(string Extension, string ContentType);

public sealed record MediaItem(string Name, string? ContentType, long Size, DateTimeOffset? UploadedAt, string? OriginalName)
{
    public string Url => MediaStore.UrlFor(Name);

    public string DisplayName => OriginalName ?? Name;
}
