// using ApexBooking.Core.Domain.Entities;
// using ApexBooking.Core.Domain.ValueObjects;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;
// using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

// namespace ApexBooking.Core.Persistence.Mappings;

// public class BookingPaymentConfiguration : IEntityTypeConfiguration<BookingPayment>
// {
//     public void Configure(EntityTypeBuilder<BookingPayment> builder)
//     {
//         builder.ToTable("booking_payments");
//         builder.HasKey(p => p.Id);
//         builder.Property(p => p.Id).HasConversion(id => id.Value, v => new BookingPaymentId(v)).HasColumnName("id");
//         builder.Property(p => p.BookingId).HasConversion(id => id.Value, v => new BookingId(v)).HasColumnName("booking_id").IsRequired();
//         builder.Property(p => p.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).HasColumnName("tenant_id").IsRequired();
//         builder.Property(p => p.Amount).HasColumnName("amount").HasPrecision(12, 2).IsRequired();
//         builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
//         builder.Property(p => p.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(20).IsRequired();
//         builder.Property(p => p.PaymentReference).HasColumnName("payment_reference").HasMaxLength(255);
//         builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
//         builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
//         builder.HasIndex(p => p.BookingId);
//         builder.HasIndex(p => p.PaymentReference);
//     }
// }

// public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
// {
//     public void Configure(EntityTypeBuilder<TenantSubscription> builder)
//     {
//         builder.ToTable("tenant_subscriptions");
//         builder.HasKey(s => s.Id);
//         builder.Property(s => s.Id).HasConversion(id => id.Value, v => new TenantSubscriptionId(v)).HasColumnName("id");
//         builder.Property(s => s.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).HasColumnName("tenant_id").IsRequired();
//         builder.Property(s => s.PlanType).HasConversion<string>().HasColumnName("plan_type").HasMaxLength(30).IsRequired();
//         builder.Property(s => s.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(20).IsRequired();
//         builder.Property(s => s.CurrentPeriodEnd).HasColumnName("current_period_end");
//         builder.Property(s => s.ProviderSubscriptionReference).HasColumnName("provider_subscription_reference").HasMaxLength(255);
//         builder.Property(s => s.ProviderCustomerReference).HasColumnName("provider_customer_reference").HasMaxLength(255);
//         builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
//         builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
//         builder.HasIndex(s => s.TenantId).IsUnique(); // one current subscription per tenant
//     }
// }

// public class TenantPaymentAccountConfiguration : IEntityTypeConfiguration<TenantPaymentAccount>
// {
//     public void Configure(EntityTypeBuilder<TenantPaymentAccount> builder)
//     {
//         builder.ToTable("tenant_payment_accounts");
//         builder.HasKey(a => a.Id);
//         builder.Property(a => a.Id).HasConversion(id => id.Value, v => new TenantPaymentAccountId(v)).HasColumnName("id");
//         builder.Property(a => a.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).HasColumnName("tenant_id").IsRequired();
//         builder.Property(a => a.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(20).IsRequired();
//         builder.Property(a => a.ProviderAccountReference).HasColumnName("provider_account_reference").HasMaxLength(255);
//         builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
//         builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").IsRequired();
//         builder.HasIndex(a => a.TenantId).IsUnique(); // one account per tenant
//     }
// }
