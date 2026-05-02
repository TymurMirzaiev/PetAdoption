using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.UnitTests.Domain;

public class MedicalRecordValueObjectTests
{
    // ──────────────────────────────────────────────────────────────
    // MicrochipId
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("12345678")]         // exactly 8 chars
    [InlineData("ABCDEF123456")]     // 12 alphanumeric
    [InlineData("00000000000000000000000")] // exactly 23 chars
    public void MicrochipId_WithValidValue_ShouldCreateSuccessfully(string value)
    {
        // Act & Assert
        var act = () => new MicrochipId(value);
        act.Should().NotThrow();
    }

    [Fact]
    public void MicrochipId_NormalizesToUppercase()
    {
        // Arrange & Act
        var id = new MicrochipId("abcdef12");

        // Assert
        id.Value.Should().Be("ABCDEF12");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MicrochipId_EmptyOrWhitespace_ThrowsInvalidMicrochipId(string value)
    {
        // Act & Assert
        var act = () => new MicrochipId(value);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidMicrochipId);
    }

    [Theory]
    [InlineData("1234567")]      // 7 chars — too short
    [InlineData("000000000000000000000001")] // 24 chars — too long
    public void MicrochipId_WrongLength_ThrowsInvalidMicrochipId(string value)
    {
        // Act & Assert
        var act = () => new MicrochipId(value);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidMicrochipId);
    }

    [Theory]
    [InlineData("ABCDEFG!")]   // special character
    [InlineData("12345678-A")] // hyphen
    public void MicrochipId_NonAlphanumeric_ThrowsInvalidMicrochipId(string value)
    {
        // Act & Assert
        var act = () => new MicrochipId(value);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidMicrochipId);
    }

    // ──────────────────────────────────────────────────────────────
    // MedicalNotes
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void MedicalNotes_WithValidValue_ShouldCreate()
    {
        // Act & Assert
        var act = () => new MedicalNotes("Regular checkups done.");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void MedicalNotes_EmptyOrWhitespace_ThrowsInvalidMedicalNotes(string value)
    {
        // Act & Assert
        var act = () => new MedicalNotes(value);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidMedicalNotes);
    }

    [Fact]
    public void MedicalNotes_ExceedsMaxLength_ThrowsInvalidMedicalNotes()
    {
        // Arrange
        var tooLong = new string('x', MedicalNotes.MaxLength + 1);

        // Act & Assert
        var act = () => new MedicalNotes(tooLong);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidMedicalNotes);
    }

    [Fact]
    public void MedicalNotes_AtMaxLength_ShouldCreate()
    {
        // Arrange
        var exactMax = new string('x', MedicalNotes.MaxLength);

        // Act & Assert
        var act = () => new MedicalNotes(exactMax);
        act.Should().NotThrow();
    }

    // ──────────────────────────────────────────────────────────────
    // Vaccination (tested via Pet.UpdateMedicalRecord + VaccinationInput)
    // ──────────────────────────────────────────────────────────────

    private static readonly Guid TestPetTypeId = Guid.NewGuid();

    private static void UpdateWithVaccination(VaccinationInput vax)
    {
        var pet = Pet.Create("Buddy", TestPetTypeId);
        pet.UpdateMedicalRecord(false, null, null, null, null, new[] { vax }, Array.Empty<string>());
    }

    [Fact]
    public void Vaccination_WithValidData_ShouldCreate()
    {
        // Arrange
        var input = new VaccinationInput("Rabies", new DateOnly(2024, 1, 15), new DateOnly(2025, 1, 15), null);

        // Act & Assert
        var act = () => UpdateWithVaccination(input);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Vaccination_EmptyVaccineType_ThrowsInvalidVaccination(string vaccineType)
    {
        // Arrange
        var input = new VaccinationInput(vaccineType, new DateOnly(2024, 1, 15), null, null);

        // Act & Assert
        var act = () => UpdateWithVaccination(input);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidVaccination);
    }

    [Fact]
    public void Vaccination_VaccineTypeTooLong_ThrowsInvalidVaccination()
    {
        // Arrange
        var tooLong = new string('x', 101);
        var input = new VaccinationInput(tooLong, new DateOnly(2024, 1, 15), null, null);

        // Act & Assert
        var act = () => UpdateWithVaccination(input);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidVaccination);
    }

    [Fact]
    public void Vaccination_NextDueBefore_AdministeredOn_ThrowsInvalidVaccination()
    {
        // Arrange
        var input = new VaccinationInput(
            "Rabies", new DateOnly(2024, 6, 15), new DateOnly(2024, 1, 1), null);

        // Act & Assert
        var act = () => UpdateWithVaccination(input);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidVaccination);
    }

    [Fact]
    public void Vaccination_NextDueSameAsAdministered_ShouldCreate()
    {
        // Arrange
        var date = new DateOnly(2024, 6, 15);
        var input = new VaccinationInput("Rabies", date, date, null);

        // Act & Assert
        var act = () => UpdateWithVaccination(input);
        act.Should().NotThrow();
    }

    [Fact]
    public void Vaccination_NotesTooLong_ThrowsInvalidVaccination()
    {
        // Arrange
        var tooLongNotes = new string('x', 501);
        var input = new VaccinationInput("Rabies", new DateOnly(2024, 1, 15), null, tooLongNotes);

        // Act & Assert
        var act = () => UpdateWithVaccination(input);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidVaccination);
    }

    // ──────────────────────────────────────────────────────────────
    // Pet.UpdateMedicalRecord
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateMedicalRecord_FirstTime_CreatesMedicalRecord()
    {
        // Arrange
        var pet = Pet.Create("Buddy", Guid.NewGuid());
        var vaccinations = new[] { new VaccinationInput("Rabies", new DateOnly(2024, 1, 1), null, null) };

        // Act
        pet.UpdateMedicalRecord(true, null, null, null, null, vaccinations, Array.Empty<string>());

        // Assert
        pet.MedicalRecord.Should().NotBeNull();
        pet.MedicalRecord!.IsSpayedNeutered.Should().BeTrue();
        pet.MedicalRecord.Vaccinations.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateMedicalRecord_SecondTime_UpdatesRecord()
    {
        // Arrange
        var pet = Pet.Create("Buddy", Guid.NewGuid());
        pet.UpdateMedicalRecord(false, null, null, null, null, Array.Empty<VaccinationInput>(), Array.Empty<string>());

        // Act
        pet.UpdateMedicalRecord(true, null, "ABCDEFGH", null, null, Array.Empty<VaccinationInput>(), new[] { "pollen" });

        // Assert
        pet.MedicalRecord!.IsSpayedNeutered.Should().BeTrue();
        pet.MedicalRecord.MicrochipId!.Value.Should().Be("ABCDEFGH");
        pet.MedicalRecord.Allergies.Should().HaveCount(1);
        pet.MedicalRecord.Allergies[0].Value.Should().Be("pollen");
    }

    [Fact]
    public void UpdateMedicalRecord_RaisesDomainEvent()
    {
        // Arrange
        var pet = Pet.Create("Buddy", Guid.NewGuid());

        // Act
        pet.UpdateMedicalRecord(false, null, null, null, null, Array.Empty<VaccinationInput>(), Array.Empty<string>());

        // Assert
        pet.DomainEvents.Should().ContainSingle(e => e is PetMedicalRecordUpdatedEvent);
    }
}
