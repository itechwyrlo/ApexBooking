using FluentValidation;
using ApexBooking.Core.Application.Features.Bookings.Commands.CreateBooking;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for CreateBookingCommand - validates dates, times, and IDs
/// </summary>
public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service ID is required")
            .Must(id => id != Guid.Empty).WithMessage("Service ID must be a valid GUID");

        RuleFor(x => x.StaffId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Staff Id, if provided, must be a valid GUID");

        RuleFor(x => x.ScheduledDate)
            .NotEmpty().WithMessage("Scheduled date is required")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Scheduled date cannot be in the past")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)))
                .WithMessage("Scheduled date cannot be more than 1 year in the future");

        RuleFor(x => x.ScheduledStartTime)
            .NotEmpty().WithMessage("Start time is required")
            .Must(time => time >= TimeOnly.MinValue && time <= TimeOnly.MaxValue)
                .WithMessage("Start time must be valid");

        RuleFor(x => x.CustomerNotes)
            .MaximumLength(1000).WithMessage("Customer notes cannot exceed 1000 characters")
            .Must(notes => string.IsNullOrEmpty(notes) || !ContainsScriptTags(notes))
                .WithMessage("Customer notes contains invalid characters");
    }

    private bool ContainsScriptTags(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        return input.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
               input.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }
}
