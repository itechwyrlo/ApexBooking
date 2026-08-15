using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class TenantRegistrationRequestConfiguration : IEntityTypeConfiguration<TenantRegistrationRequest>
    {
        public void Configure(EntityTypeBuilder<TenantRegistrationRequest> builder)
        {
            builder.ToTable("tenant_registration_request");

            builder.HasKey(r => r.TenantRegistrationRequestId);

            builder.Property(r => r.TenantRegistrationRequestId)
                .HasConversion(id => id.Value, v => new TenantRegistrationRequestId(v))
                .ValueGeneratedNever()
                .HasColumnName("tenant_registration_request_id");

            builder.Property(r => r.BusinessName).HasColumnName("business_name").IsRequired();
            builder.Property(r => r.BusinessType).HasConversion<string>().HasColumnName("business_type");
            builder.Property(r => r.RequestedSlug).HasColumnName("requested_slug").IsRequired();
            builder.Property(r => r.RequestedPlan).HasConversion<string>().HasColumnName("requested_plan");

            builder.Property(r => r.OwnerFirstName).HasColumnName("owner_first_name").IsRequired();
            builder.Property(r => r.OwnerLastName).HasColumnName("owner_last_name").IsRequired();
            builder.Property(r => r.OwnerEmail).HasColumnName("owner_email").IsRequired();

            builder.Property(r => r.Status).HasConversion<string>().HasColumnName("status");
            builder.Property(r => r.RequestedAt).HasColumnName("requested_at");
            builder.Property(r => r.ReviewedAt).HasColumnName("reviewed_at");
            builder.Property(r => r.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
            builder.Property(r => r.RejectionReason).HasColumnName("rejection_reason");

            builder.Ignore(r => r.DomainEvents);
        }
    }
}
