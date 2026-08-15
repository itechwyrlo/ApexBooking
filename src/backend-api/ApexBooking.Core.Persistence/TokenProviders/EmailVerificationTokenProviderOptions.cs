using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Core.Persistence.TokenProviders
{
    public class EmailVerificationTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public EmailVerificationTokenProviderOptions()
        {
            TokenLifespan = TimeSpan.FromHours(24); // 24 hours for email verification
        }
    }
}
