using ApexBooking.Core.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApexBooking.Core.Persistence.Seeders;

/// <summary>
/// Bootstraps the platform admin. Since ADR-055, a platform admin is just an
/// <see cref="ApplicationUser"/> with <c>IsPlatformAdmin = true</c> (no separate SuperAdmin entity),
/// so seeding goes through <see cref="UserManager{TUser}"/>.
/// </summary>
public static class SuperAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SuperAdminSeedOptions>>().Value;
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(SuperAdminSeeder));

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogInformation("SuperAdminSeed configuration missing. Skipping platform admin seed.");
            return;
        }

        var existing = await userManager.FindByEmailAsync(options.Email);
        if (existing is not null)
        {
            logger.LogInformation("Platform admin with email {Email} already exists. Skipping seed.", options.Email);
            return;
        }

        var (firstName, lastName) = ParseName(options.FullName);
        var admin = ApplicationUser.CreatePlatformAdmin(options.Email, firstName, lastName);

        var utcNow = DateTime.UtcNow;
        admin.MarkAsActive(utcNow);
        admin.MarkEmailAsConfirmed(utcNow);

        var result = await userManager.CreateAsync(admin, options.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to seed platform admin {Email}: {Errors}", options.Email, errors);
            return;
        }

        logger.LogInformation("Platform admin seeded with email {Email}.", options.Email);
    }

    private static (string FirstName, string LastName) ParseName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return ("Platform", "Admin");

        var trimmed = fullName.Trim();
        var separatorIndex = trimmed.IndexOf(' ');

        if (separatorIndex < 0)
            return (trimmed, string.Empty);

        return (trimmed[..separatorIndex], trimmed[(separatorIndex + 1)..].Trim());
    }
}
