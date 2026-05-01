namespace PetAdoption.UserService.IntegrationTests.Builders;

public class CreateOrganizationRequestBuilder
{
    private string _name = "Test Organization";
    private string _slug = "test-org";
    private string? _description = "A test organization";

    public CreateOrganizationRequestBuilder WithName(string name) { _name = name; return this; }
    public CreateOrganizationRequestBuilder WithSlug(string slug) { _slug = slug; return this; }
    public CreateOrganizationRequestBuilder WithDescription(string? description) { _description = description; return this; }

    public object Build() => new
    {
        Name = _name,
        Slug = _slug,
        Description = _description
    };

    public static CreateOrganizationRequestBuilder Default() => new();
}
