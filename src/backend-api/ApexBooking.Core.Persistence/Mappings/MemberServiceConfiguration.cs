using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class MemberServiceConfiguration : IEntityTypeConfiguration<MemberService>
    {
        public void Configure(EntityTypeBuilder<MemberService> builder)
        {
            // 1. Table and Composite Primary Key Mapping
            builder.ToTable("MemberServices");

            builder.HasKey(ms => new { ms.TenantMemberId, ms.ServiceId });

            // 2. Strongly-Typed ID Conversions (Value Objects)
            builder.Property(ms => ms.TenantMemberId)
                .HasConversion(
                    id => id.Value,
                    value => new TenantMemberId(value))
                .IsRequired();

            builder.Property(ms => ms.ServiceId)
                .HasConversion(
                    id => id.Value,
                    value => new ServiceId(value))
                .IsRequired();

            // 3. Relationship Mappings
            builder.HasOne(ms => ms.TenantMember)
                .WithMany() // Adjust if TenantMember has a navigation collection like 'public ICollection<MemberService> MemberServices { get; get; }'
                .HasForeignKey(ms => ms.TenantMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ms => ms.Service)
                .WithMany(s => s.ServiceProviders)
                .HasForeignKey(ms => ms.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .Metadata.PrincipalToDependent?.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}