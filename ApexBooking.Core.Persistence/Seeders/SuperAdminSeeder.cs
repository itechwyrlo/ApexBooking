using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Persistence.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApexBooking.Core.Persistence.Seeders;

public static class SuperAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApexBookingDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SuperAdminSeedOptions>>().Value;
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(SuperAdminSeeder));

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogInformation("SuperAdminSeed configuration missing. Skipping super admin seed.");
            return;
        }

        var exists = await context.SuperAdmins.AnyAsync(sa => sa.Email == options.Email);
        if (exists)
        {
            logger.LogInformation("Super admin with email {Email} already exists. Skipping seed.", options.Email);
            return;
        }

        var hasher = new PasswordHasher<SuperAdmin>();
        var passwordHash = hasher.HashPassword(null!, options.Password);

        var superAdmin = SuperAdmin.Create(
            string.IsNullOrWhiteSpace(options.FullName) ? "Platform Admin" : options.FullName,
            options.Email,
            passwordHash);

        context.SuperAdmins.Add(superAdmin);
        await context.SaveChangesAsync();

        logger.LogInformation("Super admin seeded with email {Email}.", options.Email);
    }
}
