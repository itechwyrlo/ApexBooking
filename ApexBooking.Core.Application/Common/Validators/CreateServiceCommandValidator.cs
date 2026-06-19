using FluentValidation;
using ApexBooking.Core.Application.Features.Services.Commands.CreateService;
using ApexBooking.Core.Application.Features.service;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for CreateServiceCommand - validates service data
/// </summary>
public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required")
            .MaximumLength(256).WithMessage("Service name cannot exceed 256 characters")
            .Matches(@"^[a-zA-Z0-9\s\-()&.]*$").WithMessage("Service name contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .Must(desc => string.IsNullOrEmpty(desc) || !ContainsScriptTags(desc))
                .WithMessage("Description contains invalid characters");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than 0")
            .LessThanOrEqualTo(480).WithMessage("Duration cannot exceed 8 hours");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative")
            .LessThanOrEqualTo(999999.99m).WithMessage("Price cannot exceed 999999.99");
    }

    private bool ContainsScriptTags(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        return input.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
               input.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }
}
