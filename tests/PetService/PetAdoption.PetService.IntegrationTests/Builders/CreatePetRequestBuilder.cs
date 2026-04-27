using PetAdoption.PetService.API.Controllers;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class CreatePetRequestBuilder
{
    private string _name = "Buddy";
    private Guid _petTypeId = Guid.NewGuid();

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

    public CreatePetRequest Build() => new(_name, _petTypeId);

    public static CreatePetRequestBuilder Default() => new();
}
