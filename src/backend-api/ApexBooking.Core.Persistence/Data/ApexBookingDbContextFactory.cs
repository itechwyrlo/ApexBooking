using ApexBooking.SharedKernel.Services;
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
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "ApexBooking.WebApi");

            var userSecretsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "UserSecrets",
                "d44beffb-c57a-438b-8995-5fee3f5a90b8",
                "secrets.json");

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile(userSecretsPath, optional: true);

            var configuration = configBuilder.Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("A connection string for 'DefaultConnection' was not found.");
            }

            var builder = new DbContextOptionsBuilder<ApexBookingDbContext>();
            builder.UseSqlServer(connectionString);

            return new ApexBookingDbContext(
                builder.Options,
                new DesignTimeTenantProvider()
                // new DummyDomainEventService()
                );
        }

        private sealed class DesignTimeTenantProvider : ITenantEntity
        {
            public TenantId? TenantId => null;
        }
    }
}