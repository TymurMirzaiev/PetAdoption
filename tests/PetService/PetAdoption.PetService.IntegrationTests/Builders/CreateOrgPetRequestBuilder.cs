using PetAdoption.PetService.API.Controllers;

namespace PetAdoption.PetService.IntegrationTests.Builders;

// See also: CreatePetRequestBuilder (same field set, different request type)
public class CreateOrgPetRequestBuilder : PetRequestBuilderBase<CreateOrgPetRequestBuilder>
{
    public CreateOrgPetRequestBuilder() : base("OrgPet") { }

    public CreateOrgPetRequest Build() => new(Name, PetTypeId, Breed, AgeMonths, Description, Tags);

    public static CreateOrgPetRequestBuilder Default() => new();
}
