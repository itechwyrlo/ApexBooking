using System.Linq.Expressions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services.Tenant;
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
        public DbSet<SuperAdminRefreshToken> SuperAdminRefreshTokens => Set<SuperAdminRefreshToken>();

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
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<FcmToken> FcmTokens => Set<FcmToken>();

        public ApexBookingDbContext(
            DbContextOptions<ApexBookingDbContext> options,
            ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
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

           ApplyGlobalFilters(builder);

        }

        // Exposed for expression tree access in BuildTenantFilter
        private TenantId? TenantContext => _tenantService?.CurrentTenant;

        private void ApplyGlobalFilters(ModelBuilder builder)
        {
            // Automatically apply tenant filter to all ITenantEntity implementations
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var companyIdProperty = Expression.Property(parameter, "CompanyId");
                    var methodCall = Expression.Call(
                        Expression.Constant(this),
                        typeof(ApexBookingDbContext).GetMethod(nameof(TenantContext)));
                    var filter = Expression.Lambda(
                        Expression.Equal(companyIdProperty, methodCall),
                        parameter);

                    builder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }
        }
    }
}