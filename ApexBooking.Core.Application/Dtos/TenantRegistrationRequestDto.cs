using System.ComponentModel.DataAnnotations;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record TenantRegistrationRequestDto
    {
        [Required]
        [StringLength(255, MinimumLength = 2)]
        public string BusinessName { get; init; } = default!;

        [Required]
        [StringLength(255, MinimumLength = 2)]
        public string OwnerFullName { get; init; } = default!;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string OwnerEmail { get; init; } = default!;

        [Required]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9\-]*$", ErrorMessage = "Slug can only contain alphanumeric characters and hyphens")]
        public string Slug { get; init; } = default!;

        [Required]
        [StringLength(50, MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; init; } = default!;

        [Required]
        [StringLength(50, MinimumLength = 10)]
        [RegularExpression(@"^[\d\s\-\+\(\)]+$", ErrorMessage = "Phone number can only contain digits, spaces, and phone symbols")]
        public string OwnerPhone { get; init; } = default!;
    }
}
