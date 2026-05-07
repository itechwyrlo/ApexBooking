using ApexBooking.SharedKernel.Services;
using ApexBooking.SharedKernel.ValueObject;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Persistence.Data
{
    public class ApexBookingDbContextFactory : IDesignTimeDbContextFactory<ApexBookingDbContext>
    {
        public ApexBookingDbContext CreateDbContext(string[] args)
        {
            // Build configuration from appsettings
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApexBookingDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            // Create a mock ITenantService for design-time
            // At design time, tenant filtering should be disabled
            var mockTenantService = new MockTenantService();

            // Create a mock logger for design-time
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<ApexBookingDbContext>();

            return new ApexBookingDbContext(optionsBuilder.Options, mockTenantService, logger);
        }

        private class MockTenantService : ITenantService
        {
            public ValueObjectTenantIdentifier.TenantId? TenantId { get; set; }
        }
    }
}
