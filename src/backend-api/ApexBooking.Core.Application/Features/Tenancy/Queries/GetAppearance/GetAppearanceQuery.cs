using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetAppearance
{
    // Plan is included alongside the appearance values themselves so the dashboard shell can
    // gate the personal dark/light toggle and the Appearance settings page can gate the
    // public-page switch from one fetch, instead of a second round-trip just for plan status.
    public record AppearanceDto(
        string ThemePaletteId,
        bool PublicPageDarkMode,
        SubscriptionPlanType Plan
    );

    public record GetAppearanceQuery() : IQuery<AppearanceDto>;
}
