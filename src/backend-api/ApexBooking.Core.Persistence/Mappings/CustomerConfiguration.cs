using ApexBooking.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;
using ApexBooking.Core.Domain.ValueObjects;

namespace ApexBooking.Core.Persistence.Mappings;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customer");

        builder.HasKey(c => c.CustomerId);

        builder.Property(c => c.CustomerId)
            .HasConversion(id => id.Value, v => new CustomerId(v))
            .HasColumnName("customer_id");

        builder.Property(c => c.TenantId)
            .HasConversion(id => id.Value, v => new TenantId(v))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.OwnsOne(c => c.Contact, contact =>
        {
            contact.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            contact.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            contact.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(50);
        });

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(c => c.TenantId);

        builder.Ignore(c => c.DomainEvents);
    }
}
