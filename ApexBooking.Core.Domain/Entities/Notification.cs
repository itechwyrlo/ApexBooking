using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class Notification : IAggregateRoot
{
    public NotificationId NotificationId { get; private set; } = default!;
    public Guid RecipientId { get; private set; }
    public NotificationRecipientType RecipientType { get; private set; }
    public TenantId? TenantId { get; private set; }
    public NotificationEventType EventType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    protected Notification() { }

    private Notification(
        Guid recipientId,
        NotificationRecipientType recipientType,
        TenantId? tenantId,
        NotificationEventType eventType,
        string title,
        string message)
    {
        NotificationId = new NotificationId(Guid.NewGuid());
        RecipientId = recipientId;
        RecipientType = recipientType;
        TenantId = tenantId;
        EventType = eventType;
        Title = title;
        Message = message;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
    }

    public static Notification Create(
        Guid recipientId,
        NotificationRecipientType recipientType,
        TenantId? tenantId,
        NotificationEventType eventType,
        string title,
        string message)
    {
        if (recipientId == Guid.Empty)
            throw new BusinessRuleBrokenException("Recipient is required.");

        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleBrokenException("Notification title is required.");

        if (string.IsNullOrWhiteSpace(message))
            throw new BusinessRuleBrokenException("Notification message is required.");

        return new Notification(recipientId, recipientType, tenantId, eventType, title, message);
    }

    public void MarkRead()
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
