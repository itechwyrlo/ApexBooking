// using ApexBooking.Core.Persistence.Identity;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;

// namespace ApexBooking.Core.Persistence.Mappings
// {
//     public sealed class ApplicationUserConfiguration
//     : IEntityTypeConfiguration<ApplicationUser>
//     {
//         public void Configure(EntityTypeBuilder<ApplicationUser> builder)
//         {
//             builder.ToTable("ApplicationUsers");

//             builder.HasKey(x => x.Id);

//             builder.Property(x => x.FullName)
//                 .IsRequired()
//                 .HasMaxLength(200);

//             builder.Property(x => x.IsPlatformAdmin)
//                 .IsRequired();

//             builder.Property(x => x.IsActive)
//                 .IsRequired();

//             builder.Property(x => x.CreatedAt)
//                 .IsRequired();

//             builder.Property(x => x.UpdatedAt)
//                 .IsRequired();

//             builder.Property(x => x.LastLoginAt);

//             builder.Property(x => x.InvitationToken)
//                 .HasMaxLength(512);

//             builder.Property(x => x.InvitationTokenExpiresAt);

//             builder.HasMany(x => x.RefreshTokens)
//                 .WithOne(x => x.ApplicationUser)
//                 .HasForeignKey(x => x.ApplicationUserId)
//                 .OnDelete(DeleteBehavior.Cascade);

//             builder.HasIndex(x => x.Email)
//                 .IsUnique();

//             builder.HasIndex(x => x.NormalizedEmail)
//                 .IsUnique();

//             builder.HasIndex(x => x.UserName)
//                 .IsUnique();

//             builder.HasIndex(x => x.NormalizedUserName)
//                 .IsUnique();

//             builder.HasIndex(x => x.IsPlatformAdmin);

//             builder.HasIndex(x => x.IsActive);

//             builder.HasIndex(x => x.InvitationToken);
//         }
//     }
// }