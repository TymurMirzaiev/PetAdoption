using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.IntegrationTests.Builders;

public class PetBuilder
{
    private string _name = "Buddy";
    private Guid _petTypeId = Guid.NewGuid();

    public PetBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PetBuilder WithPetTypeId(Guid petTypeId)
    {
        _petTypeId = petTypeId;
        return this;
    }

    public Pet Build() => Pet.Create(_name, _petTypeId);

    public static PetBuilder Default() => new();
}
