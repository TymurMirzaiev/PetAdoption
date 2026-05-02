using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.Domain;

public class Organization
{
    public Guid Id { get; private set; }
    public Address? Address { get; private set; }

    private Organization() { }

    public static Organization Create(Guid id) => new() { Id = id };

    public void SetAddress(Address address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }
}
