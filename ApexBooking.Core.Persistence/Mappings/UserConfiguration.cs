using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("user");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(u => u.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Phone)
                .HasColumnName("phone")
                .HasMaxLength(50);

            builder.Property(u => u.Role)
                .HasConversion<string>()
                .HasColumnName("role")
                .IsRequired();

            builder.Property(u => u.Status)
                .HasConversion<string>()
                .HasColumnName("status")
                .IsRequired();

            builder.Property(u => u.EmailVerifiedAt)
                .HasColumnName("email_verified_at");

            builder.Property(u => u.InvitationToken)
                .HasColumnName("invitation_token");

            builder.Property(u => u.InvitationExpiresAt)
                .HasColumnName("invitation_expires_at");

            builder.Property(u => u.LastLoginAt)
                .HasColumnName("last_login_at");

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(u => u.EmailConfirmationToken)
                .HasColumnName("email_confirmation_token")
                .IsRequired(false);

            builder.HasMany(u => u.RefreshTokens)
                .WithOne()
                .HasForeignKey(r => r.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.UserResourceAssignments)
                .WithOne()
                .HasForeignKey(ura => ura.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
         

            builder.HasIndex(u => new { u.TenantId, u.Email })
                .IsUnique();

            builder.HasIndex(u => u.TenantId);
            builder.HasIndex(u => u.Email);
            builder.HasIndex(u => u.Role);
            builder.HasIndex(u => u.Status);
        }
    }
}