using PetAdoption.PetService.API.Controllers;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class CreatePetTypeRequestBuilder
{
    private string _code = "dog";
    private string _name = "Dog";

    public CreatePetTypeRequestBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public CreatePetTypeRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreatePetTypeRequest Build() => new(_code, _name);

    public static CreatePetTypeRequestBuilder Default() => new();
}
