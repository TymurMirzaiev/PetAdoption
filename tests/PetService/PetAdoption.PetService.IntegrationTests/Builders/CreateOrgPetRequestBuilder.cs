using PetAdoption.PetService.API.Controllers;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class CreateOrgPetRequestBuilder
{
    private string _name = "OrgPet";
    private Guid _petTypeId = Guid.NewGuid();
    private string? _breed = null;
    private int? _ageMonths = null;
    private string? _description = null;
    private List<string>? _tags = null;

    public CreateOrgPetRequestBuilder WithName(string name) { _name = name; return this; }
    public CreateOrgPetRequestBuilder WithPetTypeId(Guid petTypeId) { _petTypeId = petTypeId; return this; }
    public CreateOrgPetRequestBuilder WithBreed(string breed) { _breed = breed; return this; }
    public CreateOrgPetRequestBuilder WithAgeMonths(int ageMonths) { _ageMonths = ageMonths; return this; }
    public CreateOrgPetRequestBuilder WithDescription(string description) { _description = description; return this; }
    public CreateOrgPetRequestBuilder WithTags(params string[] tags) { _tags = tags.ToList(); return this; }

    public CreateOrgPetRequest Build() => new(_name, _petTypeId, _breed, _ageMonths, _description, _tags);

    public static CreateOrgPetRequestBuilder Default() => new();
}
