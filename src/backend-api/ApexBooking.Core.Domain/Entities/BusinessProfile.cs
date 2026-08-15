using System.Collections.Generic;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.Entities
{
    // Brand metadata value object mapped onto the tenant row — physical-location
    // concerns (address, time zone, operating hours) now live on Branch.
    public record BusinessProfile
    {
        // Curated set only — no arbitrary tenant-supplied colors. Each id has a pre-verified
        // light AND dark CSS variant shipped together (see theme.css), so no combination ever
        // reaches production untested for dark-mode legibility.
        public static readonly IReadOnlySet<string> KnownPaletteIds = new HashSet<string>
        {
            "indigo", "teal", "rose", "amber", "forest", "slate"
        };

        public string BusinessName { get; private set; } = string.Empty;
        public string? Logo { get; private set; }
        public BusinessType BusinessType { get; private set; }
        public string? Description { get; private set; }
        // Shown to customers on the public refund-status page — deliberately separate from
        // Tenant.OwnerContact's phone number, which is the owner's personal contact, not a
        // business-facing one.
        public string? ContactPhoneNumber { get; private set; }

        // "indigo" matches this app's existing default brand color — an unconfigured tenant's
        // dashboard/public page look exactly as they do today.
        public string ThemePaletteId { get; private set; } = "indigo";
        public bool PublicPageDarkMode { get; private set; }

        private BusinessProfile() { }

        internal static BusinessProfile CreateDefault(string name, BusinessType type) => new()
        {
            BusinessName = name.Trim(),
            BusinessType = type
        };

        public void UpdateDetails(string name, string? description, string? logoUrl, string? contactPhoneNumber)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessRuleBrokenException("Business name is required.");

            BusinessName = name.Trim();
            Description = description?.Trim();
            Logo = logoUrl;
            ContactPhoneNumber = contactPhoneNumber?.Trim();
        }

        // tenantCanUseDarkMode is passed in by the caller (driven by Tenant.Plan) rather than
        // resolved here — this value object has no way to see the aggregate root's other state,
        // and the caller already has it on hand from the same load.
        public void UpdateAppearance(string themePaletteId, bool publicPageDarkMode, bool tenantCanUseDarkMode)
        {
            if (!KnownPaletteIds.Contains(themePaletteId))
                throw new BusinessRuleBrokenException("Unrecognized theme palette.");

            // Defense in depth — the UI already hides/disables this control on the Basic plan,
            // but a direct API call must be rejected too, not just trusted from the client.
            if (publicPageDarkMode && !tenantCanUseDarkMode)
                throw new BusinessRuleBrokenException("Dark mode requires the Professional plan or above.");

            ThemePaletteId = themePaletteId;
            PublicPageDarkMode = publicPageDarkMode;
        }
    }
}
