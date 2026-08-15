using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.Appearance
{
    public record UpdateAppearanceCommand(
        string ThemePaletteId,
        bool PublicPageDarkMode
    ) : ICommand;
}
