namespace PetAdoption.PetService.IntegrationTests.Builders;

/// <summary>
/// Shared base for pet-creation request builders.
/// <para>
/// Both <see cref="CreatePetRequestBuilder"/> and <see cref="CreateOrgPetRequestBuilder"/> build
/// records with an identical field set; this base centralises the mutable state and With… methods
/// so each concrete builder only needs to supply its own default name and <c>Build()</c> override.
/// </para>
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type (for fluent return types).</typeparam>
public abstract class PetRequestBuilderBase<TBuilder>
    where TBuilder : PetRequestBuilderBase<TBuilder>
{
    protected string Name;
    protected Guid PetTypeId = Guid.NewGuid();
    protected string? Breed;
    protected int? AgeMonths;
    protected string? Description;
    protected List<string>? Tags;

    protected PetRequestBuilderBase(string defaultName)
    {
        Name = defaultName;
    }

    public TBuilder WithName(string name) { Name = name; return (TBuilder)this; }
    public TBuilder WithPetTypeId(Guid petTypeId) { PetTypeId = petTypeId; return (TBuilder)this; }
    public TBuilder WithBreed(string breed) { Breed = breed; return (TBuilder)this; }
    public TBuilder WithAgeMonths(int ageMonths) { AgeMonths = ageMonths; return (TBuilder)this; }
    public TBuilder WithDescription(string description) { Description = description; return (TBuilder)this; }
    public TBuilder WithTags(params string[] tags) { Tags = tags.ToList(); return (TBuilder)this; }
}
