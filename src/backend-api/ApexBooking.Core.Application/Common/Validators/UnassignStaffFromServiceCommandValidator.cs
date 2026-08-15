using ApexBooking.Core.Application.Features.Services.Commands.UnassignStaffFromService;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for UnassignStaffFromServiceCommand - validates identifiers are present
/// </summary>
public class UnassignStaffFromServiceCommandValidator : AbstractValidator<UnassignStaffFromServiceCommand>
{
    public UnassignStaffFromServiceCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service id is required");

        RuleFor(x => x.TenantMemberId)
            .NotEmpty().WithMessage("Team member id is required");
    }
}
