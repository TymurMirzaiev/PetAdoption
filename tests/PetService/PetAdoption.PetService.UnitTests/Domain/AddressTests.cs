using FluentAssertions;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.UnitTests.Domain;

public class AddressTests
{
    // ──────────────────────────────────────────────────────────────
    // Valid construction
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidData_ShouldCreateAddress()
    {
        // Arrange & Act
        var address = new Address(52.52m, 13.405m, "Alexanderplatz 1", "Berlin", "BE", "Germany", "10178");

        // Assert
        address.Lat.Should().Be(52.52m);
        address.Lng.Should().Be(13.405m);
        address.City.Should().Be("Berlin");
        address.Country.Should().Be("Germany");
    }

    [Fact]
    public void Constructor_WithBoundaryLatLng_ShouldCreateAddress()
    {
        // Arrange & Act
        var address = new Address(90m, 180m, "Edge Location", "EdgeCity", "", "EdgeLand", "");

        // Assert
        address.Lat.Should().Be(90m);
        address.Lng.Should().Be(180m);
    }

    [Fact]
    public void Constructor_WithNegativeBoundaryLatLng_ShouldCreateAddress()
    {
        // Arrange & Act
        var address = new Address(-90m, -180m, "South Pole", "Nowhere", "", "Antarctica", "");

        // Assert
        address.Lat.Should().Be(-90m);
        address.Lng.Should().Be(-180m);
    }

    // ──────────────────────────────────────────────────────────────
    // Lat validation
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(90.001)]
    [InlineData(91)]
    [InlineData(180)]
    public void Constructor_WithLatAbove90_ShouldThrow(double lat)
    {
        // Act & Assert
        var act = () => new Address((decimal)lat, 0m, "Line1", "City", "", "Country", "");
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOrganizationAddress);
    }

    [Theory]
    [InlineData(-90.001)]
    [InlineData(-91)]
    [InlineData(-180)]
    public void Constructor_WithLatBelow90_ShouldThrow(double lat)
    {
        // Act & Assert
        var act = () => new Address((decimal)lat, 0m, "Line1", "City", "", "Country", "");
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOrganizationAddress);
    }

    // ──────────────────────────────────────────────────────────────
    // Lng validation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithLngAbove180_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Address(0m, 180.001m, "Line1", "City", "", "Country", "");
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOrganizationAddress);
    }

    [Fact]
    public void Constructor_WithLngBelow180_ShouldThrow()
    {
        // Act & Assert
        var act = () => new Address(0m, -180.001m, "Line1", "City", "", "Country", "");
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOrganizationAddress);
    }

    // ──────────────────────────────────────────────────────────────
    // Required string validation
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyLine1_ShouldThrow(string? line1)
    {
        // Act & Assert
        var act = () => new Address(0m, 0m, line1!, "City", "", "Country", "");
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOrganizationAddress);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyCity_ShouldThrow(string? city)
    {
        // Act & Assert
        var act = () => new Address(0m, 0m, "Line1", city!, "", "Country", "");
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOrganizationAddress);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithEmptyCountry_ShouldThrow(string? country)
    {
        // Act & Assert
        var act = () => new Address(0m, 0m, "Line1", "City", "", country!, "");
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidOrganizationAddress);
    }

    [Fact]
    public void Constructor_WithEmptyRegionAndPostalCode_ShouldCreateAddress()
    {
        // Arrange & Act — Region and PostalCode are optional
        var address = new Address(0m, 0m, "Main St", "Anytown", "", "USA", "");

        // Assert
        address.Region.Should().Be(string.Empty);
        address.PostalCode.Should().Be(string.Empty);
    }
}
