namespace PetAdoption.UserService.Domain.ValueObjects;

public record UserId
{
    public string Value { get; init; }

    private UserId(string value) => Value = value;

    public static UserId Create() => new(Guid.NewGuid().ToString());

    public static UserId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("UserId cannot be empty", nameof(value));

        return new UserId(value);
    }

    public override string ToString() => Value;
}
