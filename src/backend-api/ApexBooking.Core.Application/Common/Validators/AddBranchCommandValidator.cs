using System.Text.RegularExpressions;
using ApexBooking.Core.Application.Common.ReferenceData.Psgc;
using ApexBooking.Core.Application.Features.Tenancy.Commands.Branches;
using ApexBooking.Core.Domain.Services;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for AddBranchCommand - validates branch name, time zone, and address
/// </summary>
public class AddBranchCommandValidator : AbstractValidator<AddBranchCommand>
{
    private static readonly Regex PhilippineZipCodePattern = new(@"^\d{4}$", RegexOptions.Compiled);
    private readonly IPsgcReferenceService _psgcReferenceService;

    public AddBranchCommandValidator(IPsgcReferenceService psgcReferenceService)
    {
        _psgcReferenceService = psgcReferenceService;

        RuleFor(x => x.BranchName)
            .NotEmpty().WithMessage("Branch name is required")
            .MaximumLength(200).WithMessage("Branch name cannot exceed 200 characters");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Time zone is required")
            .Must(BranchTimeZoneConverter.IsValidTimeZoneId).WithMessage("Enter a valid IANA time zone identifier (e.g. 'Asia/Manila').");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City or municipality is required");

        RuleFor(x => x.Province)
            .NotEmpty().WithMessage("Province is required");

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("ZIP code is required")
            .Matches(PhilippineZipCodePattern).WithMessage("A Philippine ZIP code must be exactly four digits");

        RuleFor(x => x)
            .Custom((command, context) =>
            {
                if (string.IsNullOrWhiteSpace(command.Province) || string.IsNullOrWhiteSpace(command.City))
                    return;

                var error = _psgcReferenceService.ValidateAddress(command.Province, command.City, command.Barangay);
                if (error != null)
                    context.AddFailure(error.FieldName, error.Message);
            });
    }
}
