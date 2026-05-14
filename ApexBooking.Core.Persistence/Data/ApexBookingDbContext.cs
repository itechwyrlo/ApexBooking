using System.Linq.Expressions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.SharedKernel.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Data
{
    public class ApexBookingDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        private readonly ITenantService _tenantService;
        private readonly ILogger<ApexBookingDbContext> _logger;


        // Platform entities
        public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();

        // Tenant entities
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<TenantRequest> TenantRequests => Set<TenantRequest>();
        public DbSet<TenantProfile> TenantProfiles => Set<TenantProfile>();
        public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
        public DbSet<TenantPaymentPolicy> TenantPaymentPolicies => Set<TenantPaymentPolicy>();

        // Service entities
        public DbSet<Service> Services => Set<Service>();
        public DbSet<ServiceStaff> ServiceStaffs => Set<ServiceStaff>();

        // Resource entities
        public DbSet<Staff> Staffs => Set<Staff>();
        public DbSet<StaffAvailabilitySchedule> StaffAvailabilitySchedules => Set<StaffAvailabilitySchedule>();
        public DbSet<StaffBreakPeriod> StaffBreakPeriods => Set<StaffBreakPeriod>();
        public DbSet<StaffAvailabilityException> StaffAvailabilityExceptions => Set<StaffAvailabilityException>();

        // Booking entities
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingStatusLog> BookingStatusLogs => Set<BookingStatusLog>();
        public DbSet<Guest> Guests => Set<Guest>();
        public DbSet<GuestCancellationToken> GuestCancellationTokens => Set<GuestCancellationToken>();

        // User entities
        public DbSet<User> Users => Set<User>();
        public DbSet<UserResourceAssignment> UserResourceAssignments => Set<UserResourceAssignment>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
        public DbSet<PlatformPaymentGateway> PlatformPaymentGateways => Set<PlatformPaymentGateway>();

        public ApexBookingDbContext(
            DbContextOptions<ApexBookingDbContext> options,
            ITenantService tenantService,
            ILogger<ApexBookingDbContext> logger) : base(options)
        {
            _tenantService = tenantService;
            _logger = logger;
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(ApexBookingDbContext).Assembly);

            // Rename Identity tables to match ERD


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

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    _logger.LogInformation("Applying tenant filter to entity type: {EntityType}", entityType.ClrType.Name);
                    _logger.LogInformation("Current tenant context: {TenantId}", _tenantService.TenantId?.Value ?? (object)"null");
                    builder.Entity(entityType.ClrType)
                        .HasQueryFilter(BuildTenantFilter(entityType.ClrType));
                }
            }




        }

        // Exposed for expression tree access in BuildTenantFilter
        private TenantId? TenantContext => _tenantService?.TenantId;

        private LambdaExpression BuildTenantFilter(Type entityType)
        {
            // Only skip at design-time (EF migrations) when no DI container is present
            if (_tenantService == null)
                return null;

            var param = Expression.Parameter(entityType, "e");
            var entityTenantId = Expression.Property(param, nameof(ITenantEntity.TenantId));

            // Access TenantContext via 'this' — EF Core substitutes the current DbContext
            // instance at query execution time, so each request reads its own tenant ID.
            // Using Expression.Constant(_tenantService) directly would permanently capture
            // the first request's scoped instance (bug fixed here).
            var dbContextExpr = Expression.Constant(this, typeof(ApexBookingDbContext));
            var currentTenantId = Expression.Property(dbContextExpr, nameof(TenantContext));
            var nullTenantId = Expression.Constant(null, typeof(TenantId));

            // TenantContext == null  →  no active tenant (super admin) — skip filter
            // TenantContext != null  →  e.TenantId == TenantContext
            var isNoContext = Expression.Equal(currentTenantId, nullTenantId);
            var isSameTenant = Expression.Equal(entityTenantId, currentTenantId);

            return Expression.Lambda(Expression.OrElse(isNoContext, isSameTenant), param);
        }
    }
}