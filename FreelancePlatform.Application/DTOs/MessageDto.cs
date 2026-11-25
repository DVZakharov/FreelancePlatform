using FreelancePlatform.Domain.Entities;

namespace FreelancePlatform.Application.DTOs;

public record MessageDto(
    Guid Id,
    Guid ContractId,
    Guid SenderId,
    string SenderName,
    string Content,
    DateTime CreatedAt)
{
    public static MessageDto FromEntity(MessageEntity entity) =>
        new(
            entity.Id,
            entity.ContractId,
            entity.SenderId,
            entity.Sender.FullName,
            entity.Content,
            entity.CreatedAt);
}