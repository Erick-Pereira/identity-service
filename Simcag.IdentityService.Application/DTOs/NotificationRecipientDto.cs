namespace Simcag.IdentityService.Application.DTOs;

public sealed class NotificationRecipientDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public sealed class NotificationRecipientsResponseDto
{
    public Guid TenantId { get; init; }
    public IReadOnlyList<NotificationRecipientDto> Recipients { get; init; } = Array.Empty<NotificationRecipientDto>();
}
