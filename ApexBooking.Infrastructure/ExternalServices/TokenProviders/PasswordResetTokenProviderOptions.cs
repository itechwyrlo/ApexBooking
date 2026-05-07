using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Infrastructure.ExternalServices.TokenProviders
{
    public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public PasswordResetTokenProviderOptions()
        {
            TokenLifespan = TimeSpan.FromHours(1); // 1 hour for password reset
        }
    }
}
