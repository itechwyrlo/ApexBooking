using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.ValueObjects;

/// <summary>
/// The tenant's trial window (05 §1). Set when <c>Tenant.Approve()</c> starts the trial.
/// Trial expiry does not change the tenant's <c>Status</c> — an expired-trial tenant can
/// still log in; the restriction is enforced where bookings are created (05 §1 / ADR-058).
/// </summary>
public record TrialPeriod
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    private TrialPeriod() { }

    public TrialPeriod(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new BusinessRuleBrokenException("Trial end date must be after the start date.");

        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>Starts a trial of <paramref name="days"/> days from <paramref name="from"/>.</summary>
    public static TrialPeriod StartingFrom(DateTime from, int days) => new(from, from.AddDays(days));

    public bool IsExpiredAt(DateTime nowUtc) => nowUtc >= EndDate;
}
