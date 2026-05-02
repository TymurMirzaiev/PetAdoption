using PetAdoption.PetService.API.Controllers;

namespace PetAdoption.PetService.IntegrationTests.Builders;

// See also: CreateOrgPetRequestBuilder (same field set, different request type)
public class CreatePetRequestBuilder : PetRequestBuilderBase<CreatePetRequestBuilder>
{
    public CreatePetRequestBuilder() : base("Buddy") { }

    public CreatePetRequest Build() => new(Name, PetTypeId, Breed, AgeMonths, Description, Tags);

    public static CreatePetRequestBuilder Default() => new();
}
