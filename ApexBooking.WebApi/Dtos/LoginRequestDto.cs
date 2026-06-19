using System.ComponentModel.DataAnnotations;

namespace ApexBooking.WebApi.Dtos
{
    public record LoginRequestDto(
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        string Email,
        
        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        string Password
    );
}
