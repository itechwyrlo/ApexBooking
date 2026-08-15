using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Persistence.Identity;

namespace ApexBooking.Core.Persistence.Mappings
{
    public sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.RefreshTokenSecret)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(x => x.IsUsed)
                .IsRequired();

            builder.Property(x => x.IsRevoked)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ExpiryDate)
                .IsRequired();

            builder.HasOne(x => x.ApplicationUser)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.RefreshTokenSecret)
                .IsUnique();

            builder.HasIndex(x => x.ApplicationUserId);

            builder.HasIndex(x => x.ExpiryDate);

            builder.HasIndex(x => new
            {
                x.ApplicationUserId,
                x.IsRevoked,
                x.IsUsed
            });
        }
    }
}
