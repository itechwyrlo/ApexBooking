using System.ComponentModel.DataAnnotations;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; }
    }
}
