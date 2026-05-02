namespace PetAdoption.PetService.Application.DTOs;

public record ChatMessageDto(
    Guid Id,
    Guid AdoptionRequestId,
    Guid SenderUserId,
    string SenderRole,
    string Body,
    DateTime SentAt,
    DateTime? ReadByRecipientAt);
