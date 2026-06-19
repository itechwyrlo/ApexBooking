using FluentValidation;
using ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for CancelBookingCommand - validates reason
/// </summary>
public class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters")
            .Must(reason => string.IsNullOrEmpty(reason) || !ContainsScriptTags(reason))
                .WithMessage("Reason contains invalid characters");
    }

    private bool ContainsScriptTags(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        return input.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
               input.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }
}
