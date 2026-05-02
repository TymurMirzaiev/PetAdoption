using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Domain.ValueObjects;

public class Address
{
    public decimal Lat { get; private set; }
    public decimal Lng { get; private set; }
    public string Line1 { get; private set; }
    public string City { get; private set; }
    public string Region { get; private set; }
    public string Country { get; private set; }
    public string PostalCode { get; private set; }

    private Address() { }

    public Address(decimal lat, decimal lng, string line1, string city, string region, string country, string postalCode)
    {
        if (lat < -90 || lat > 90)
            throw new DomainException(PetDomainErrorCode.InvalidOrganizationAddress, "Latitude must be between -90 and 90.");
        if (lng < -180 || lng > 180)
            throw new DomainException(PetDomainErrorCode.InvalidOrganizationAddress, "Longitude must be between -180 and 180.");
        if (string.IsNullOrWhiteSpace(line1))
            throw new DomainException(PetDomainErrorCode.InvalidOrganizationAddress, "Line1 is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException(PetDomainErrorCode.InvalidOrganizationAddress, "City is required.");
        if (string.IsNullOrWhiteSpace(country))
            throw new DomainException(PetDomainErrorCode.InvalidOrganizationAddress, "Country is required.");

        Lat = lat;
        Lng = lng;
        Line1 = line1.Trim();
        City = city.Trim();
        Region = region?.Trim() ?? string.Empty;
        Country = country.Trim();
        PostalCode = postalCode?.Trim() ?? string.Empty;
    }
}
