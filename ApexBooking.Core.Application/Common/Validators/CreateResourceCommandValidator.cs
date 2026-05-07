using FluentValidation;
using ApexBooking.Core.Application.Features.Availability.Commands.CreateResource;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for CreateResourceCommand - validates resource data
/// </summary>
public class CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Resource name is required")
            .MaximumLength(256).WithMessage("Resource name cannot exceed 256 characters")
            .Matches(@"^[a-zA-Z0-9\s\-()&.]*$").WithMessage("Resource name contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .Must(desc => string.IsNullOrEmpty(desc) || !ContainsScriptTags(desc))
                .WithMessage("Description contains invalid characters");

        RuleFor(x => x.ResourceType)
            .IsInEnum()
            .WithMessage("Please select a valid resource type.");

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
