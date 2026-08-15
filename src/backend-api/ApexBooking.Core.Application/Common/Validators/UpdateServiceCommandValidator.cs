using ApexBooking.Core.Application.Features.Services.Commands.UpdateService;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for UpdateServiceCommand - validates catalog entry name, duration, price, and currency
/// </summary>
public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service name is required")
            .MaximumLength(200).WithMessage("Service name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Service duration must be greater than zero minutes");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Service price cannot be negative");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required")
            .Length(3).WithMessage("A valid 3-character ISO 4217 currency code is required");

        RuleFor(x => x.BufferBeforeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Time buffer windows cannot be negative integers");

        RuleFor(x => x.BufferAfterMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Time buffer windows cannot be negative integers");

        RuleFor(x => x.MinAdvanceBookingHoursOverride)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum advance booking hours cannot be negative")
            .When(x => x.MinAdvanceBookingHoursOverride.HasValue);
    }
}
