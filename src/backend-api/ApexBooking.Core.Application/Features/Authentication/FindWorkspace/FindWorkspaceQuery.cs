using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Authentication.FindWorkspace
{
    // Slug is null for every "not found" reason alike — unknown email, platform admin (no tenant
    // membership at all), inactive tenant, inactive membership. Deliberately not distinguished, so
    // this endpoint never discloses which of those is actually true for a given email.
    public record FindWorkspaceResult(string? Slug);

    public record FindWorkspaceQuery(string Email) : IQuery<FindWorkspaceResult>;
}
