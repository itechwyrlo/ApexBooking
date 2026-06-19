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
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");

            var optionsBuilder = new DbContextOptionsBuilder<ApexBookingDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            var mockTenantService = new MockTenantService();

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