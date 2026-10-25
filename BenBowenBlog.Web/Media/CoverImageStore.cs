using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BenBowenBlog.Web.Media;

// Cover images live in the private "covers" blob container and are served by MediaEndpoints at /media/{name}.
public sealed class CoverImageStore(BlobServiceClient blobServiceClient)
{
    private readonly BlobContainerClient container = blobServiceClient.GetBlobContainerClient("covers");

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

    public async Task<string> UploadAsync(byte[] data, ImageFormat format, CancellationToken cancellationToken = default)
    {
        var name = $"{Guid.NewGuid():N}.{format.Extension}";

        await container.GetBlobClient(name).UploadAsync(
            new BinaryData(data),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = format.ContentType } },
            cancellationToken);

        return name;
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
