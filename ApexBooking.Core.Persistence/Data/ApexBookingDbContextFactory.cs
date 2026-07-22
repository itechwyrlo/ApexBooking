using ApexBooking.Core.Domain.Services.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Data
{
    public class ApexBookingDbContextFactory : IDesignTimeDbContextFactory<ApexBookingDbContext>
    {
        public ApexBookingDbContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Booking.WebApi");

            var userSecretsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "UserSecrets",
                "1a733462-f06d-46fe-b8e3-41705dbec538",
                "secrets.json");

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile(userSecretsPath, optional: true);

            var configuration = configBuilder.Build();

            var connectionString = configuration.GetConnectionString("Dev_Booking");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("A connection string for 'Dev_Booking' was not found.");
            }

            var builder = new DbContextOptionsBuilder<ApexBookingDbContext>();
            builder.UseSqlServer(connectionString);

            return new ApexBookingDbContext(
                builder.Options,
                new DesignTimeTenantProvider()
                // new DummyDomainEventService()
                );
        }

        private sealed class DesignTimeTenantProvider : ITenantService
        {
            // Explicitly implement or back the interface property
            public TenantId CurrentTenant => null!;

            // Implement the required interface method
            public void SetCurrentTenant(TenantId tenant)
            {
            }
        }
    }
}