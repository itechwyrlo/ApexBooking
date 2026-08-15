using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Authentication.GetWorkspaceSummary
{
    public record GetWorkspaceSummaryResult(string BusinessName, string? Logo);

    // No parameters — slug travels as a route segment (/api/{slug}/workspace-summary), and
    // TenantMiddleware already resolves it into the ambient ITenantEntity for any unauthenticated
    // request whose route has a {slug} segment, same mechanism TenantLoginCommand relies on.
    public record GetWorkspaceSummaryQuery : IQuery<GetWorkspaceSummaryResult>;
}
