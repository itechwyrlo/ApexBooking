using ApexBooking.Core.Persistence.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApexBooking.Core.Persistence.TokenProviders
{
    public class PasswordResetTokenProvider : DataProtectorTokenProvider<ApplicationUser>
    {
        public PasswordResetTokenProvider(IDataProtectionProvider dataProtectionProvider, 
            IOptions<PasswordResetTokenProviderOptions> options, 
            ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }
}
