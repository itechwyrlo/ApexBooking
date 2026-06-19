using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings;

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> builder)
    {
        builder.ToTable("guests");

        builder.HasKey(g => g.GuestId);

        builder.Property(g => g.GuestId)
            .HasConversion(id => id.Value, v => new GuestId(v))
            .HasColumnName("id");

        builder.Property(g => g.TenantId)
            .HasConversion(id => id.Value, v => new TenantId(v))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(g => g.BookingId)
            .HasConversion(id => id.Value, v => new BookingId(v))
            .HasColumnName("booking_id")
            .IsRequired();

        builder.Property(g => g.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(g => g.Phone)
            .HasColumnName("phone")
            .HasMaxLength(30);

        builder.Property(g => g.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        // Guest → Booking: FK on Guest.BookingId; cascade when Booking is deleted
        builder.HasOne(g => g.Booking)
            .WithOne(b => b.Guest)
            .HasForeignKey<Guest>(g => g.BookingId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => g.BookingId).IsUnique();
        builder.HasIndex(g => new { g.TenantId, g.Email });
    }
}
