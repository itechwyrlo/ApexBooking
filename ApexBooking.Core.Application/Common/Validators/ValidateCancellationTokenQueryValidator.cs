using ApexBooking.Core.Application.Features.Bookings.Queries.ValidateCancellationToken;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class ValidateCancellationTokenQueryValidator : AbstractValidator<ValidateCancellationTokenQuery>
{
    public ValidateCancellationTokenQueryValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.");
    }
}
