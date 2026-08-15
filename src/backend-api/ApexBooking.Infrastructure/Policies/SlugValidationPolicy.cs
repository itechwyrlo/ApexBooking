using System.Text.RegularExpressions;
using ApexBooking.Core.Domain.Policies;

namespace ApexBooking.Infrastructure.Policies
{
    public sealed class SlugValidationPolicy : ISlugValidationPolicy
    {
        // Regex: Only alphanumeric characters and single internal hyphens
        private static readonly Regex SlugRegex = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

        // List of blacklisted slugs to protect system stability
        private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "api", "admin", "administrator", "billing", "support", "mail", "portal", "dev", "staging"
    };

        public bool IsValid(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            string cleanSlug = slug.ToLowerInvariant().Trim();

            // Rule A: Length must be between 3 and 63 characters
            if (cleanSlug.Length < 3 || cleanSlug.Length > 63)
                return false;

            // Rule B: Cannot match system reserved platform words
            if (ReservedSlugs.Contains(cleanSlug))
                return false;

            // Rule C: Must strictly match URL safe character rules
            return SlugRegex.IsMatch(cleanSlug);
        }
    }
}
