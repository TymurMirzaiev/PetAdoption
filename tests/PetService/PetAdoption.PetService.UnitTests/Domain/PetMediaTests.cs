using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.UnitTests.Domain;

public class PetMediaTests
{
    private static readonly Guid TestPetTypeId = Guid.NewGuid();

    private static Pet CreatePet() => Pet.Create("Buddy", TestPetTypeId);

    // ──────────────────────────────────────────────────────────────
    // AddPhoto
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddPhoto_FirstPhoto_SetsPrimary()
    {
        // Arrange
        var pet = CreatePet();
        var mediaId = Guid.NewGuid();

        // Act
        pet.AddPhoto(mediaId, "https://example.com/photo.jpg", "image/jpeg");

        // Assert
        pet.Media.Should().HaveCount(1);
        pet.Media[0].IsPrimary.Should().BeTrue();
        pet.Media[0].MediaType.Should().Be(PetMediaType.Photo);
        pet.Media[0].SortOrder.Should().Be(0);
    }

    [Fact]
    public void AddPhoto_SecondPhoto_NotPrimary()
    {
        // Arrange
        var pet = CreatePet();
        pet.AddPhoto(Guid.NewGuid(), "https://example.com/1.jpg", "image/jpeg");

        // Act
        pet.AddPhoto(Guid.NewGuid(), "https://example.com/2.jpg", "image/jpeg");

        // Assert
        pet.Media.Should().HaveCount(2);
        pet.Media[0].IsPrimary.Should().BeTrue();
        pet.Media[1].IsPrimary.Should().BeFalse();
        pet.Media[1].SortOrder.Should().Be(1);
    }

    [Fact]
    public void AddPhoto_WithEmptyUrl_Throws()
    {
        // Arrange
        var pet = CreatePet();

        // Act & Assert
        var act = () => pet.AddPhoto(Guid.NewGuid(), "", "image/jpeg");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddPhoto_WithEmptyContentType_Throws()
    {
        // Arrange
        var pet = CreatePet();

        // Act & Assert
        var act = () => pet.AddPhoto(Guid.NewGuid(), "https://example.com/photo.jpg", "");
        act.Should().Throw<ArgumentException>();
    }

    // ──────────────────────────────────────────────────────────────
    // AddVideo
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddVideo_WhenNoVideoExists_AddsVideo()
    {
        // Arrange
        var pet = CreatePet();
        var mediaId = Guid.NewGuid();

        // Act
        pet.AddVideo(mediaId, "https://example.com/video.mp4", "video/mp4");

        // Assert
        pet.Media.Should().HaveCount(1);
        pet.Media[0].MediaType.Should().Be(PetMediaType.Video);
        pet.Media[0].IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void AddVideo_WhenVideoExists_ThrowsVideoAlreadyExists()
    {
        // Arrange
        var pet = CreatePet();
        pet.AddVideo(Guid.NewGuid(), "https://example.com/video.mp4", "video/mp4");

        // Act & Assert
        var act = () => pet.AddVideo(Guid.NewGuid(), "https://example.com/video2.mp4", "video/mp4");
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.VideoAlreadyExists);
    }

    [Fact]
    public void AddVideo_WithPhotosPresent_AddsSuccessfully()
    {
        // Arrange
        var pet = CreatePet();
        pet.AddPhoto(Guid.NewGuid(), "https://example.com/photo.jpg", "image/jpeg");

        // Act
        pet.AddVideo(Guid.NewGuid(), "https://example.com/video.mp4", "video/mp4");

        // Assert
        pet.Media.Should().HaveCount(2);
        pet.Media.Count(m => m.MediaType == PetMediaType.Video).Should().Be(1);
    }

    // ──────────────────────────────────────────────────────────────
    // RemoveMedia
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveMedia_PrimaryPhoto_PromotesNextPhoto()
    {
        // Arrange
        var pet = CreatePet();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        pet.AddPhoto(firstId, "https://example.com/1.jpg", "image/jpeg");
        pet.AddPhoto(secondId, "https://example.com/2.jpg", "image/jpeg");

        // Act
        pet.RemoveMedia(firstId);

        // Assert
        pet.Media.Should().HaveCount(1);
        pet.Media[0].Id.Should().Be(secondId);
        pet.Media[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void RemoveMedia_NonPrimaryPhoto_DoesNotChangePrimary()
    {
        // Arrange
        var pet = CreatePet();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        pet.AddPhoto(firstId, "https://example.com/1.jpg", "image/jpeg");
        pet.AddPhoto(secondId, "https://example.com/2.jpg", "image/jpeg");

        // Act
        pet.RemoveMedia(secondId);

        // Assert
        pet.Media.Should().HaveCount(1);
        pet.Media[0].Id.Should().Be(firstId);
        pet.Media[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void RemoveMedia_NotFound_ThrowsMediaNotFound()
    {
        // Arrange
        var pet = CreatePet();

        // Act & Assert
        var act = () => pet.RemoveMedia(Guid.NewGuid());
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.MediaNotFound);
    }

    [Fact]
    public void RemoveMedia_Video_RemovesSuccessfully()
    {
        // Arrange
        var pet = CreatePet();
        var videoId = Guid.NewGuid();
        pet.AddVideo(videoId, "https://example.com/video.mp4", "video/mp4");

        // Act
        pet.RemoveMedia(videoId);

        // Assert
        pet.Media.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // ReorderPhotos
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ReorderPhotos_ValidOrder_UpdatesSortOrder()
    {
        // Arrange
        var pet = CreatePet();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        pet.AddPhoto(firstId, "https://example.com/1.jpg", "image/jpeg");
        pet.AddPhoto(secondId, "https://example.com/2.jpg", "image/jpeg");

        // Act
        pet.ReorderPhotos(new[] { secondId, firstId });

        // Assert
        var reordered = pet.Media.Where(m => m.MediaType == PetMediaType.Photo)
            .OrderBy(m => m.SortOrder).ToList();
        reordered[0].Id.Should().Be(secondId);
        reordered[0].SortOrder.Should().Be(0);
        reordered[1].Id.Should().Be(firstId);
        reordered[1].SortOrder.Should().Be(1);
    }

    [Fact]
    public void ReorderPhotos_MismatchedIds_ThrowsInvalidMediaOrder()
    {
        // Arrange
        var pet = CreatePet();
        pet.AddPhoto(Guid.NewGuid(), "https://example.com/1.jpg", "image/jpeg");

        // Act & Assert
        var act = () => pet.ReorderPhotos(new[] { Guid.NewGuid() });
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidMediaOrder);
    }

    [Fact]
    public void ReorderPhotos_MissingIds_ThrowsInvalidMediaOrder()
    {
        // Arrange
        var pet = CreatePet();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        pet.AddPhoto(firstId, "https://example.com/1.jpg", "image/jpeg");
        pet.AddPhoto(secondId, "https://example.com/2.jpg", "image/jpeg");

        // Act & Assert
        var act = () => pet.ReorderPhotos(new[] { firstId });
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidMediaOrder);
    }

    // ──────────────────────────────────────────────────────────────
    // SetPrimaryPhoto
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void SetPrimaryPhoto_OnPhoto_ChangesPromoted()
    {
        // Arrange
        var pet = CreatePet();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        pet.AddPhoto(firstId, "https://example.com/1.jpg", "image/jpeg");
        pet.AddPhoto(secondId, "https://example.com/2.jpg", "image/jpeg");

        // Act
        pet.SetPrimaryPhoto(secondId);

        // Assert
        pet.Media.First(m => m.Id == secondId).IsPrimary.Should().BeTrue();
        pet.Media.First(m => m.Id == firstId).IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void SetPrimaryPhoto_OnVideo_ThrowsMediaNotPhoto()
    {
        // Arrange
        var pet = CreatePet();
        var videoId = Guid.NewGuid();
        pet.AddVideo(videoId, "https://example.com/video.mp4", "video/mp4");

        // Act & Assert
        var act = () => pet.SetPrimaryPhoto(videoId);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.MediaNotPhoto);
    }

    [Fact]
    public void SetPrimaryPhoto_NotFound_ThrowsMediaNotFound()
    {
        // Arrange
        var pet = CreatePet();

        // Act & Assert
        var act = () => pet.SetPrimaryPhoto(Guid.NewGuid());
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.MediaNotFound);
    }
}
