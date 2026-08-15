using ApexBooking.Core.Application.Features.Services.Commands.AssignStaffToService;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for AssignStaffToServiceCommand - validates identifiers are present
/// </summary>
public class AssignStaffToServiceCommandValidator : AbstractValidator<AssignStaffToServiceCommand>
{
    public AssignStaffToServiceCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service id is required");

        RuleFor(x => x.TenantMemberId)
            .NotEmpty().WithMessage("Team member id is required");
    }
}
