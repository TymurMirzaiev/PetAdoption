using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.UnitTests.Domain;

public class ChatMessageTests
{
    // ──────────────────────────────���───────────────────────────��───
    // Send — body validation
    // ─────────────────────────────────────────��────────────────────

    [Fact]
    public void Send_WithBodyTooLong_ThrowsInvalidChatMessageBody()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var longBody = new string('x', 2001);

        // Act & Assert
        var act = () => ChatMessage.Send(request, Guid.NewGuid(), ChatSenderRole.Adopter, longBody);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidChatMessageBody);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Send_WithEmptyBody_ThrowsInvalidChatMessageBody(string? body)
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        var act = () => ChatMessage.Send(request, Guid.NewGuid(), ChatSenderRole.Adopter, body!);
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.InvalidChatMessageBody);
    }

    // ────────────────────────────────────────────────────────��─────
    // Send — terminal state rejection
    // ──────────────────────────────────────────���───────────────────

    [Fact]
    public void Send_OnRejectedRequest_ThrowsChatThreadClosed()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Reject("Not a fit.");

        // Act & Assert
        var act = () => ChatMessage.Send(request, Guid.NewGuid(), ChatSenderRole.Adopter, "Hello");
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.ChatThreadClosed);
    }

    [Fact]
    public void Send_OnCancelledRequest_ThrowsChatThreadClosed()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        request.Cancel();

        // Act & Assert
        var act = () => ChatMessage.Send(request, Guid.NewGuid(), ChatSenderRole.Adopter, "Hello");
        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be(PetDomainErrorCode.ChatThreadClosed);
    }

    // ─────────────────────────────���──────────────────────────────��─
    // Send — success
    // ────────────────────────────────────────���─────────────────────

    [Fact]
    public void Send_OnPendingRequest_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = AdoptionRequest.Create(userId, Guid.NewGuid(), Guid.NewGuid());

        // Act
        var message = ChatMessage.Send(request, userId, ChatSenderRole.Adopter, "  Hello!  ");

        // Assert
        message.Id.Should().NotBeEmpty();
        message.AdoptionRequestId.Should().Be(request.Id);
        message.SenderUserId.Should().Be(userId);
        message.SenderRole.Should().Be(ChatSenderRole.Adopter);
        message.Body.Should().Be("Hello!"); // trimmed
        message.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        message.ReadByRecipientAt.Should().BeNull();
    }

    [Fact]
    public void Send_OnApprovedRequest_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = AdoptionRequest.Create(userId, Guid.NewGuid(), Guid.NewGuid());
        request.Approve();

        // Act
        var message = ChatMessage.Send(request, userId, ChatSenderRole.Adopter, "Thanks!");

        // Assert
        message.Should().NotBeNull();
        message.Body.Should().Be("Thanks!");
    }

    // ──────────────────────────────────────────────────────────────
    // MarkRead
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void MarkRead_IsIdempotent()
    {
        // Arrange
        var request = AdoptionRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var message = ChatMessage.Send(request, Guid.NewGuid(), ChatSenderRole.Adopter, "Hello");
        var firstTime = DateTime.UtcNow;
        message.MarkRead(firstTime);

        // Act
        var laterTime = DateTime.UtcNow.AddMinutes(5);
        message.MarkRead(laterTime);

        // Assert — value should not change after first set
        message.ReadByRecipientAt.Should().Be(firstTime);
    }
}
