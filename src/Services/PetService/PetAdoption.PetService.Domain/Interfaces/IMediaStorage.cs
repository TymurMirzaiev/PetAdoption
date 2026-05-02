namespace PetAdoption.PetService.Domain.Interfaces;

public interface IMediaStorage
{
    Task<MediaUploadResult> UploadAsync(Stream content, string contentType, string fileName, CancellationToken ct);
    Task DeleteAsync(string url, CancellationToken ct);
}

public record MediaUploadResult(string Url, string StorageKey, long SizeBytes);
