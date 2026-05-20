using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class SuperAdminConfiguration : IEntityTypeConfiguration<SuperAdmin>
    {
        public void Configure(EntityTypeBuilder<SuperAdmin> builder)
        {
            builder.ToTable("super_admin");

            builder.HasKey(x => x.SuperAdminId);

            builder.Property(x => x.SuperAdminId)
                .HasConversion(id => id.Value, v => new SuperAdminId(v));

            builder.Property(x => x.FullName).HasColumnName("full_name");
            builder.Property(x => x.Email).HasColumnName("email");
            builder.Property(x => x.PasswordHash).HasColumnName("password_hash");

            builder.Property(x => x.IsActive).HasColumnName("is_active");

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            builder.HasIndex(x => x.Email).IsUnique();

            builder.HasMany(x => x.RefreshTokens)
                .WithOne()
                .HasForeignKey(x => x.SuperAdminId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
