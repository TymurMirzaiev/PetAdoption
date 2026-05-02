namespace PetAdoption.PetService.Infrastructure.Storage;

public class MediaStorageOptions
{
    public string Provider { get; set; } = "LocalDisk";
    public string? LocalDiskBasePath { get; set; }
}
