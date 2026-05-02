using Microsoft.Extensions.Options;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Storage;

public class LocalDiskMediaStorage : IMediaStorage
{
    private readonly string _basePath;

    public LocalDiskMediaStorage(IOptions<MediaStorageOptions> options)
    {
        var configuredPath = options.Value.LocalDiskBasePath;
        _basePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "wwwroot", "media")
            : configuredPath;
    }

    public async Task<MediaUploadResult> UploadAsync(
        Stream content, string contentType, string fileName, CancellationToken ct)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = ContentTypeToExtension(contentType);

        var mediaId = Guid.NewGuid();
        var relativeFilePath = $"{mediaId}{ext}";

        Directory.CreateDirectory(_basePath);

        var absoluteFilePath = Path.Combine(_basePath, relativeFilePath);

        await using var fileStream = new FileStream(absoluteFilePath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fileStream, ct);

        var sizeBytes = fileStream.Length;
        var url = $"/media/{relativeFilePath}";
        var storageKey = relativeFilePath;

        return new MediaUploadResult(url, storageKey, sizeBytes);
    }

    public Task DeleteAsync(string url, CancellationToken ct)
    {
        // url is like /media/{fileName}
        var fileName = Path.GetFileName(url);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var absolutePath = Path.Combine(_basePath, fileName);
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private static string ContentTypeToExtension(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "video/mp4" => ".mp4",
        _ => ".bin"
    };
}
