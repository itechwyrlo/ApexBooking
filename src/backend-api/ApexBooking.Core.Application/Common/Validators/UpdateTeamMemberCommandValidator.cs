using ApexBooking.Core.Application.Features.Tenancy.Commands.UpdateTeamMember;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for UpdateTeamMemberCommand - validates identifier, name, and role
/// </summary>
public class UpdateTeamMemberCommandValidator : AbstractValidator<UpdateTeamMemberCommand>
{
    public UpdateTeamMemberCommandValidator()
    {
        RuleFor(x => x.TenantMemberId)
            .NotEmpty().WithMessage("Team member id is required");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        // Owner is a legal value here — Tenant.UpdateMemberProfile allows resubmitting the
        // current owner's own unchanged Owner role (so they can edit their other fields) and
        // only rejects an actual promotion/demotion, which this field-level rule can't see
        // (it has no access to the target member's current role). That invariant is enforced
        // domain-side, not here.
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("A valid role is required");
    }
}
