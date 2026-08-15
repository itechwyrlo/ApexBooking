using ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentGateway;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for SetPaymentGatewayWebhookSecretCommand - mirrors the domain check in
/// TenantPaymentCredential.SetWebhookSecret
/// </summary>
public class SetPaymentGatewayWebhookSecretCommandValidator : AbstractValidator<SetPaymentGatewayWebhookSecretCommand>
{
    public SetPaymentGatewayWebhookSecretCommandValidator()
    {
        RuleFor(x => x.WebhookSecret)
            .NotEmpty().WithMessage("PayMongo webhook signing secret is required")
            .Must(secret => secret.StartsWith("whsk_"))
            .WithMessage("A valid PayMongo webhook signing secret starting with 'whsk_' is required")
            .When(x => !string.IsNullOrWhiteSpace(x.WebhookSecret));
    }
}
