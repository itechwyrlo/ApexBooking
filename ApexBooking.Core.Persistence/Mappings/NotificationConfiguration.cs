using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.NotificationId);

        builder.Property(n => n.NotificationId)
            .HasConversion(id => id.Value, v => new NotificationId(v))
            .HasColumnName("id");

        builder.Property(n => n.RecipientId)
            .HasColumnName("recipient_id")
            .IsRequired();

        builder.Property(n => n.RecipientType)
            .HasConversion<string>()
            .HasColumnName("recipient_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.TenantId)
            .HasConversion(
                id => id != null ? (Guid?)id.Value : null,
                v => v.HasValue ? new TenantId(v.Value) : null)
            .HasColumnName("tenant_id");

        builder.Property(n => n.EventType)
            .HasConversion<string>()
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(n => n.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasColumnName("message")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(n => n.ReadAt)
            .HasColumnName("read_at");

        builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt });
    }
}
