namespace PetAdoption.PetService.Infrastructure.Storage;

public record MediaStorageOptions(string Provider = "LocalDisk", string? LocalDiskBasePath = null);
