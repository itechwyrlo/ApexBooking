using ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentGateway;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for ConfigurePaymentGatewayCommand - validates PayMongo key prefixes, mirroring
/// the domain check in TenantPaymentCredential.UpdateCredentials
/// </summary>
public class ConfigurePaymentGatewayCommandValidator : AbstractValidator<ConfigurePaymentGatewayCommand>
{
    public ConfigurePaymentGatewayCommandValidator()
    {
        RuleFor(x => x.SecretKey)
            .NotEmpty().WithMessage("PayMongo secret key is required")
            .Must(key => key.StartsWith("sk_test_") || key.StartsWith("sk_live_"))
            .WithMessage("A valid PayMongo secret key starting with 'sk_test_' or 'sk_live_' is required")
            .When(x => !string.IsNullOrWhiteSpace(x.SecretKey));

        RuleFor(x => x.PublicKey)
            .NotEmpty().WithMessage("PayMongo public key is required")
            .Must(key => key.StartsWith("pk_test_") || key.StartsWith("pk_live_"))
            .WithMessage("A valid PayMongo public key starting with 'pk_test_' or 'pk_live_' is required")
            .When(x => !string.IsNullOrWhiteSpace(x.PublicKey));
    }
}
