using ApexBooking.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings;

public class SmsUsageConfiguration : IEntityTypeConfiguration<SmsUsage>
{
    public void Configure(EntityTypeBuilder<SmsUsage> builder)
    {
        builder.ToTable("sms_usage");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.TenantId)
            .HasConversion(id => id.Value, v => new TenantId(v))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(u => u.PeriodYear).HasColumnName("period_year").IsRequired();
        builder.Property(u => u.PeriodMonth).HasColumnName("period_month").IsRequired();
        builder.Property(u => u.SentCount).HasColumnName("sent_count").IsRequired();

        // One row per tenant per month (ADR-056).
        builder.HasIndex(u => new { u.TenantId, u.PeriodYear, u.PeriodMonth }).IsUnique();
    }
}
