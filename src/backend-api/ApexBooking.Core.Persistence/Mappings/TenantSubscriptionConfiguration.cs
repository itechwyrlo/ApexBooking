using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
    {
        public void Configure(EntityTypeBuilder<TenantSubscription> builder)
        {
            // 1. Table and Primary Key Mapping
            builder.ToTable("TenantSubscriptions");
            
            builder.HasKey(ts => ts.Id);

            // 2. Strongly-Typed ID Conversions (Value Objects)
            builder.Property(ts => ts.Id)
                .HasConversion(
                    id => id.Value,
                    value => new TenantSubscriptionId(value))
                .IsRequired();

            builder.Property(ts => ts.TenantId)
                .HasConversion(
                    id => id.Value,
                    value => new TenantId(value))
                .IsRequired();

            // 3. Property Constraints and Requirements
            builder.Property(ts => ts.PlanType)
                .HasConversion<string>() // Stores Plan Enum as string (e.g., Free, Premium, Enterprise)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(ts => ts.Status)
                .HasConversion<string>() // Stores Status Enum as string (e.g., Trialing, Active, PastDue, Canceled)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(ts => ts.CurrentPeriodEnd); // Nullable, matches your model

            builder.Property(ts => ts.ProviderSubscriptionReference)
                .HasMaxLength(255); // Length to support subscription tracking codes (e.g., Stripe sub_xyz)

            builder.Property(ts => ts.ProviderCustomerReference)
                .HasMaxLength(255); // Length to support customer gateway profiles (e.g., Stripe cus_xyz)

            // 4. Audit Properties
            builder.Property(ts => ts.CreatedAt)
                .IsRequired();

            builder.Property(ts => ts.UpdatedAt)
                .IsRequired();
        }
    }
}
