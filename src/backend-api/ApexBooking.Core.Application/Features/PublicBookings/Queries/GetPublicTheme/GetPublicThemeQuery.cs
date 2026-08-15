using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicTheme
{
    // Deliberately minimal — just enough to render the correct CSS palette/mode before anything
    // else loads. Business name/logo/banner display is a separate, later piece of work.
    public record PublicThemeDto(
        string ThemePaletteId,
        bool IsDarkMode
    );

    public record GetPublicThemeQuery() : IQuery<PublicThemeDto>;
}
