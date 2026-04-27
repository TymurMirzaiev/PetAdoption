using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class PetTypeBuilder
{
    private string _code = "dog";
    private string _name = "Dog";

    public PetTypeBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public PetTypeBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PetType Build() => PetType.Create(_code, _name);

    public static PetTypeBuilder Default() => new();
}
