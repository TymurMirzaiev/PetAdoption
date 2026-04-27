using PetAdoption.PetService.API.Controllers;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class UpdatePetRequestBuilder
{
    private string _name = "UpdatedName";

    public UpdatePetRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UpdatePetRequest Build() => new(_name);

    public static UpdatePetRequestBuilder Default() => new();
}
