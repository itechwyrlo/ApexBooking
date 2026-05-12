using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBooking.Core.Persistence.Mappings;

public class TenantRequestConfiguration : IEntityTypeConfiguration<TenantRequest>
{
    public void Configure(EntityTypeBuilder<TenantRequest> builder)
    {
        builder.ToTable("tenant_requests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(id => id.Value, v => new TenantRequestId(v))
            .HasColumnName("id");

        builder.Property(r => r.BusinessName)
            .HasColumnName("business_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.OwnerFullName)
            .HasColumnName("owner_full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.OwnerEmail)
            .HasColumnName("owner_email")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.OwnerPhone)
            .HasColumnName("owner_phone")
            .HasMaxLength(50);

        builder.Property(r => r.Plan)
            .HasConversion<string>()
            .HasColumnName("plan")
            .IsRequired();

        builder.Property(r => r.Message)
            .HasColumnName("message")
            .HasMaxLength(1000);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .IsRequired();

        builder.Property(r => r.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.ReviewedAt)
            .HasColumnName("reviewed_at");

        builder.HasIndex(r => r.OwnerEmail);
    }
}
