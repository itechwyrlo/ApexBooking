using ApexBooking.Core.Application.Features.SuperAdmin.Commands.ConfigurePlatformPaymentGateway;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class ConfigurePlatformPaymentGatewayCommandValidator
    : AbstractValidator<ConfigurePlatformPaymentGatewayCommand>
{
    public ConfigurePlatformPaymentGatewayCommandValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("Client ID is required.")
            .MaximumLength(200).WithMessage("Client ID cannot exceed 200 characters.");

        RuleFor(x => x.SecretKey)
            .NotEmpty().WithMessage("Secret key is required.")
            .MaximumLength(200).WithMessage("Secret key cannot exceed 200 characters.");

        RuleFor(x => x.WebhookId)
            .NotEmpty().WithMessage("Webhook ID is required.")
            .MaximumLength(255).WithMessage("Webhook ID cannot exceed 255 characters.");
    }
}
