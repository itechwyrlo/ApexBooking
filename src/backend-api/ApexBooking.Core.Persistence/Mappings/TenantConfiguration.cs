using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("tenant");

            builder.HasKey(t => t.TenantId);

            builder.Property(t => t.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .ValueGeneratedNever()
                .HasColumnName("tenant_id");

            builder.Property(t => t.Slug)
                .HasColumnName("slug")
                .HasMaxLength(255)
                .IsRequired();

            builder.OwnsOne(t => t.OwnerContact, oc =>
            {
                oc.Property(p => p.FirstName).HasColumnName("owner_first_name").HasMaxLength(255).IsRequired();
                oc.Property(p => p.LastName).HasColumnName("owner_last_name").HasMaxLength(255).IsRequired();
                oc.Property(p => p.Email).HasColumnName("owner_email").HasMaxLength(255).IsRequired();
                oc.Property(p => p.PhoneNumber).HasColumnName("owner_phone_number").HasMaxLength(50);
            });

            builder.Property(t => t.Plan)
                .HasConversion<string>()
                .HasColumnName("plan")
                .IsRequired();

            builder.Property(t => t.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            builder.Property(t => t.SetupRequired)
                .HasColumnName("setup_required")
                .IsRequired();

            builder.Property(t => t.SetupCompleted)
                .HasColumnName("setup_completed")
                .IsRequired();

            builder.OwnsOne(t => t.Trial, tp =>
            {
                tp.Property(p => p.StartDate).HasColumnName("trial_started_at");
                tp.Property(p => p.EndDate).HasColumnName("trial_ended_at");
            });

            builder.Property(t => t.TrialExpiredAt).HasColumnName("trial_expired_at");
            builder.Property(t => t.TrialReminderSentAt).HasColumnName("trial_reminder_sent_at");
            builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Property(t => t.DeactivatedAt).HasColumnName("deactivated_at");

            // Brand identity flattened onto the tenant row — physical-location
            // concerns (address, time zone, operating hours) live on Branch.
            builder.OwnsOne(t => t.BusinessProfile, bp =>
            {
                bp.Property(p => p.BusinessName).HasColumnName("business_name").HasMaxLength(255).IsRequired();
                bp.Property(p => p.Logo).HasColumnName("logo_url").HasMaxLength(2048);
                bp.Property(p => p.BusinessType).HasConversion<string>().HasColumnName("business_type").HasMaxLength(50).IsRequired();
                bp.Property(p => p.Description).HasColumnName("description").HasMaxLength(1000);
                bp.Property(p => p.ContactPhoneNumber).HasColumnName("contact_phone_number").HasMaxLength(50);
                bp.Property(p => p.ThemePaletteId).HasColumnName("theme_palette_id").HasMaxLength(30).IsRequired();
                bp.Property(p => p.PublicPageDarkMode).HasColumnName("public_page_dark_mode").IsRequired();
            });
            builder.Navigation(t => t.BusinessProfile).IsRequired();

            builder.HasOne(t => t.BookingPolicy)
                .WithOne(b => b.Tenant)
                .HasForeignKey<BookingPolicy>(b => b.TenantId)
                .IsRequired(false);

            builder.HasOne(t => t.PaymentPolicy)
                .WithOne(p => p.Tenant)
                .HasForeignKey<PaymentPolicy>(p => p.TenantId)
                .IsRequired(false);

            builder.HasOne(t => t.PaymentCredential)
                .WithOne()
                .HasForeignKey<TenantPaymentCredential>(p => p.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Map Team Members child collection
            builder.HasMany(t => t.Members)
                .WithOne()
                .HasForeignKey(tm => tm.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(Tenant.Members))?
                .SetField("_tenantMembers");
            builder.Metadata.FindNavigation(nameof(Tenant.Members))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);


            builder.HasMany(t => t.Services)
                .WithOne()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(Tenant.Services))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(t => t.Branches)
                .WithOne()
                .HasForeignKey(b => b.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(Tenant.Branches))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(t => t.Bookings)
                .WithOne()
                .HasForeignKey(b => b.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(Tenant.Bookings))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.Ignore(t => t.DomainEvents);

            builder.HasIndex(t => t.Slug).IsUnique();
        }
    }
}
