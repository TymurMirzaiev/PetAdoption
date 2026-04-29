using PetAdoption.PetService.API.Controllers;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class UpdatePetRequestBuilder
{
    private string _name = "UpdatedName";
    private string? _breed = null;
    private int? _ageMonths = null;
    private string? _description = null;

    public UpdatePetRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UpdatePetRequestBuilder WithBreed(string breed)
    {
        _breed = breed;
        return this;
    }

    public UpdatePetRequestBuilder WithAgeMonths(int ageMonths)
    {
        _ageMonths = ageMonths;
        return this;
    }

    public UpdatePetRequestBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public UpdatePetRequest Build() => new(_name, _breed, _ageMonths, _description);

    public static UpdatePetRequestBuilder Default() => new();
}
