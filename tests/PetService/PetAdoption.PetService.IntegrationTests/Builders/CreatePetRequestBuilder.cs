using PetAdoption.PetService.API.Controllers;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class CreatePetRequestBuilder
{
    private string _name = "Buddy";
    private Guid _petTypeId = Guid.NewGuid();
    private string? _breed = null;
    private int? _ageMonths = null;
    private string? _description = null;
    private List<string>? _tags = null;

    public CreatePetRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreatePetRequestBuilder WithPetTypeId(Guid petTypeId)
    {
        _petTypeId = petTypeId;
        return this;
    }

    public CreatePetRequestBuilder WithBreed(string breed)
    {
        _breed = breed;
        return this;
    }

    public CreatePetRequestBuilder WithAgeMonths(int ageMonths)
    {
        _ageMonths = ageMonths;
        return this;
    }

    public CreatePetRequestBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CreatePetRequestBuilder WithTags(params string[] tags)
    {
        _tags = tags.ToList();
        return this;
    }

    public CreatePetRequest Build() => new(_name, _petTypeId, _breed, _ageMonths, _description, _tags);

    public static CreatePetRequestBuilder Default() => new();
}
