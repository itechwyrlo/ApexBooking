using System.Linq.Expressions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Persistence.Identity;
using ApexBooking.Core.Persistence.Mappings;
using ApexBooking.SharedKernel.Services;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Data
{
    public class ApexBookingDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IDataProtectionKeyContext
    {
        private readonly ITenantEntity _tenantEntity;
        private readonly ILogger<ApexBookingDbContext> _logger;
        private readonly ISecretProtector _secretProtector;

        private TenantId? CurrentTenantId => _tenantEntity.TenantId;
        // Platform entities
       public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
       public DbSet<TenantRegistrationRequest> TenantRegistrationRequests => Set<TenantRegistrationRequest>();
        // Tenant entities
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<PaymentPolicy> TenantPaymentPolicies => Set<PaymentPolicy>();
        public DbSet<TenantPaymentCredential> TenantPaymentCredentials => Set<TenantPaymentCredential>();

        // Business entities
    
        // Customer entities
        public DbSet<Customer> Customers => Set<Customer>();

        // Service entities
        public DbSet<Service> Services => Set<Service>();

        // Staff entities
        public DbSet<TenantMember> Staffs => Set<TenantMember>();
        public DbSet<StaffBreak> StaffBreaks => Set<StaffBreak>();
        public DbSet<StaffTimeOffRequest> StaffTimeOffRequests => Set<StaffTimeOffRequest>();

        // Booking entities
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<FcmToken> FcmTokens => Set<FcmToken>();

        // Not tenant-scoped by design — the outbox relay runs with no ambient tenant and must see
        // pending rows across every tenant. See OutboxMessage's own doc comment.
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        // SMS quota counter (ADR-056) — a persistence record, not an aggregate.
        public DbSet<SmsUsage> SmsUsages => Set<SmsUsage>();
        public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
        public DbSet<TenantPaymentAccount> TenantPaymentAccounts => Set<TenantPaymentAccount>();

        public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();

        // Idempotency ledger for PayMongo webhook deliveries — see ProcessedPaymentEvent.
        public DbSet<ProcessedPaymentEvent> ProcessedPaymentEvents => Set<ProcessedPaymentEvent>();

        // Data Protection key ring (ISecretProtector's backing store) — see IDataProtectionKeyContext.
        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

        public ApexBookingDbContext(DbContextOptions<ApexBookingDbContext> options, ITenantEntity tenantEntity, ISecretProtector secretProtector)
           : base(options)
        {
            _tenantEntity = tenantEntity;
            _secretProtector = secretProtector;
        }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // Rename Identity tables to match ERD
             builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("ApplicationUsers");
                entity.Property(u => u.PhotoUrl).HasMaxLength(2048);
            });


            builder.Entity<IdentityRole<Guid>>(entity =>
            {
                entity.ToTable("roles");
            });

            builder.Entity<IdentityUserRole<Guid>>(entity =>
            {
                entity.ToTable("user_roles");
            });

            builder.Entity<IdentityUserClaim<Guid>>(entity =>
            {
                entity.ToTable("user_claims");
            });

            builder.Entity<IdentityUserLogin<Guid>>(entity =>
            {
                entity.ToTable("user_logins");
            });

            builder.Entity<IdentityUserToken<Guid>>(entity =>
            {
                entity.ToTable("user_tokens");
            });

            builder.Entity<IdentityRoleClaim<Guid>>(entity =>
            {
                entity.ToTable("role_claims");
            });

            // TenantPaymentCredentialConfiguration needs a constructor-injected ISecretProtector,
            // which the parameterless-constructor assembly scan below can't provide — excluded from
            // the scan and applied explicitly instead. Every other IEntityTypeConfiguration<> in
            // this assembly is unaffected.
            builder.ApplyConfigurationsFromAssembly(typeof(ApexBookingDbContext).Assembly,
                t => t != typeof(TenantPaymentCredentialConfiguration));
            builder.ApplyConfiguration(new TenantPaymentCredentialConfiguration(_secretProtector));

            ApplyGlobalFilters(builder);

        }

        private void ApplyGlobalFilters(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes().ToList())
            {
                if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    // 1. Configure properties
                    builder.Entity(entityType.ClrType)
                        .Property<TenantId>(nameof(ITenantEntity.TenantId))
                        .HasConversion(
                            id => id.Value,
                            value => new TenantId(value))
                        .IsRequired();

                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(ITenantEntity.TenantId));

                    // Capture 'this.CurrentTenantId' securely
                    var contextExpression = Expression.Constant(this);
                    var currentTenantProperty = Expression.Property(contextExpression, nameof(CurrentTenantId));

                    var noAmbientTenant = Expression.Equal(
                        currentTenantProperty,
                        Expression.Constant(null, typeof(TenantId)));
                    var tenantMatches = Expression.Equal(property, currentTenantProperty);
                    var comparison = Expression.OrElse(noAmbientTenant, tenantMatches);
                    var lambda = Expression.Lambda(comparison, parameter);

                    builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }
    }
}