using System.ComponentModel.DataAnnotations;

namespace ApexBooking.Core.Application.Dtos
{
    public record ResetPasswordRequestDto
    {
        [Required]
        public Guid UserId { get; init; }

        [Required]
        public string Token { get; init; }

        [Required]
        [MinLength(8)]
        public string NewPassword { get; init; }

        [Required]
        public string ConfirmPassword { get; init; }
    }
}
