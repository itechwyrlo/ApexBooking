using ApexBooking.Core.Domain.Services;
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

            // Design-time only: CI has none of the sources above (no user-secrets, no populated
            // appsettings.*.json), but dotnet-ef still needs *a* syntactically valid connection
            // string to pick the SQL Server provider dialect for model-building/script generation
            // — it never actually connects for that. Deliberately checked LAST and under a name
            // distinct from the standard ConnectionStrings__DefaultConnection env var convention,
            // so this can never be mistaken for (or collide with) how the real app resolves its
            // actual runtime connection string.
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = Environment.GetEnvironmentVariable("EF_DESIGNTIME_CONNECTION_STRING");
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("A connection string for 'DefaultConnection' was not found.");
            }

            var builder = new DbContextOptionsBuilder<ApexBookingDbContext>();
            builder.UseSqlServer(connectionString);

            return new ApexBookingDbContext(
                builder.Options,
                new DesignTimeTenantProvider(),
                new DesignTimeSecretProtector()
                // new DummyDomainEventService()
                );
        }

        private sealed class DesignTimeTenantProvider : ITenantEntity
        {
            public TenantId? TenantId => null;
        }

        // Design-time only (dotnet ef migrations/database commands) — never used to read or write
        // real data, only to build the model, so a real Data Protection key ring is unnecessary
        // here. An identity pass-through is enough for EF's tooling to construct the DbContext.
        private sealed class DesignTimeSecretProtector : ISecretProtector
        {
            public string Protect(string plaintext) => plaintext;
            public string Unprotect(string ciphertext) => ciphertext;
        }
    }
}