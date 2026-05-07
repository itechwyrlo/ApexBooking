using ApexBooking.Infrastructure.Configuration;

namespace ApexBooking.WebApi.Extensions;

/// <summary>
/// Extension for validating that required configuration and secrets are properly loaded
/// </summary>
public static class ConfigurationValidationExtensions
{
    public static IServiceCollection AddConfigurationValidation(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Validate JWT configuration
        var jwtSection = configuration.GetSection("Jwt");
        if (string.IsNullOrEmpty(jwtSection["Issuer"]) || 
            string.IsNullOrEmpty(jwtSection["Audience"]))
        {
            throw new InvalidOperationException(
                "JWT Issuer and Audience must be configured. Set via environment variables JWT:Issuer and JWT:Audience");
        }

        // Validate that JWT keys are loaded from environment (not hardcoded)
        var jwtOptions = jwtSection.Get<JwtOptions>();
        if (string.IsNullOrEmpty(jwtOptions?.PrivateKeyPem) ||
            string.IsNullOrEmpty(jwtOptions?.PublicKeyPem))
        {
            throw new InvalidOperationException(
                "JWT Private and Public keys must be loaded from environment variables. " +
                "Set JWT:PrivateKeyPem and JWT:PublicKeyPem via environment variables or User Secrets");
        }

        // Validate connection string in non-Development environments
        if (!environment.IsDevelopment())
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' must be configured for production. " +
                    "Set via environment variable ConnectionStrings:DefaultConnection");
            }
        }

        // Validate external service credentials if configured
        var externalServices = configuration.GetSection("BrevoSmtp").Get<BrevoSmtpOptions>();
        if (externalServices != null && !string.IsNullOrEmpty(externalServices.Key))
        {
            // If SMTP is configured, ensure we have required settings
            var emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>();
            if (emailSettings == null || string.IsNullOrEmpty(emailSettings.SenderEmail))
            {
                throw new InvalidOperationException(
                    "If email (Brevo SMTP) is configured, EmailSettings:SenderEmail must be set");
            }
        }

        return services;
    }
}
