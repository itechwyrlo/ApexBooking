using ApexBooking.Core.Domain.Enums;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Services.Notification;

/// <summary>
/// The per-tenant monthly SMS quota (ADR-056). Domain interface, Persistence impl (needs DbContext).
/// </summary>
public interface ISmsQuotaService
{
    /// <summary>
    /// Atomically reserves one SMS for the current month if the tenant is under its plan limit.
    /// Returns false at limit — the caller then skips the SMS. Reserves first, sends second.
    /// </summary>
    Task<bool> TryConsumeAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves per-plan limits (ADR-053) — never stored per-tenant, so a plan's limits can change for
/// everyone without touching data. MVP SMS quotas: Basic 100 / Professional 500 / Enterprise custom.
/// </summary>
public interface IPlanPolicy
{
    int GetSmsMonthlyLimit(SubscriptionPlanType plan);
}
