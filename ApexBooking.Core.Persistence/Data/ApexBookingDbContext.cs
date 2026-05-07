using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Persistence.Mappings;
using ApexBooking.SharedKernel.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
        public DbSet<TenantProfile> TenantProfiles => Set<TenantProfile>();
        public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

        // Service entities
        public DbSet<Service> Services => Set<Service>();
        public DbSet<ServiceResource> ServiceResources => Set<ServiceResource>();

        // Resource entities
        public DbSet<Resource> Resources => Set<Resource>();
        public DbSet<ResourceAvailabilitySchedule> ResourceAvailabilitySchedules => Set<ResourceAvailabilitySchedule>();
        public DbSet<ResourceBreakPeriod> ResourceBreakPeriods => Set<ResourceBreakPeriod>();
        public DbSet<ResourceAvailabilityException> ResourceAvailabilityExceptions => Set<ResourceAvailabilityException>();

        // Booking entities
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingStatusLog> BookingStatusLogs => Set<BookingStatusLog>();

        // User entities
        public DbSet<User> Users => Set<User>();
        public DbSet<UserResourceAssignment> UserResourceAssignments => Set<UserResourceAssignment>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

        private LambdaExpression BuildTenantFilter(Type entityType)
        {
            // At design time (migrations), _tenantService may be null
            // In this case, skip tenant filtering
            if (_tenantService == null)
            {
                return null;
            }

            // If tenant context is not set, skip tenant filtering
            // This prevents WHERE 0 = 1 when tenant context is null
            if (_tenantService.TenantId == null)
            {
                return null;
            }

            var param = Expression.Parameter(entityType, "e");
            var property = Expression.Property(param, nameof(ITenantEntity.TenantId));
            var tenantId = Expression.Property(
                Expression.Constant(_tenantService),
                nameof(ITenantService.TenantId));
            var body = Expression.Equal(property, tenantId);
            return Expression.Lambda(body, param);
        }
    }
}