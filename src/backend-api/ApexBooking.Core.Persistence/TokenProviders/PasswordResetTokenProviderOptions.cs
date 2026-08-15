using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Core.Persistence.TokenProviders
{
    public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public PasswordResetTokenProviderOptions()
        {
            TokenLifespan = TimeSpan.FromHours(1); // 1 hour for password reset
        }
    }
}
