using FluentValidation;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Application.Features.Staffs.Commands.CreateStaff;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for CreateResourceCommand - validates resource data
/// </summary>
public class CreateResourceCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateResourceCommandValidator()
    {
         RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("FirstName name is required")
            .MaximumLength(256).WithMessage("Resource name cannot exceed 256 characters")
            .Matches(@"^[a-zA-Z0-9\s\-()&.]*$").WithMessage("FirstName name contains invalid characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("LastName name is required")
            .MaximumLength(256).WithMessage("Resource name cannot exceed 256 characters")
            .Matches(@"^[a-zA-Z0-9\s\-()&.]*$").WithMessage("LastName name contains invalid characters");

        RuleFor(x => x.email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.contactNumber)
            .NotEmpty().WithMessage("Contact number is required.")
            .MaximumLength(255).WithMessage("Contact number cannot exceed 255 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .Must(desc => string.IsNullOrEmpty(desc) || !ContainsScriptTags(desc))
                .WithMessage("Description contains invalid characters");


        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than 0");
    }

    private bool ContainsScriptTags(string? input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        return input.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
               input.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }
}
