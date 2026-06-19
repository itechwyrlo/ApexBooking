using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Infrastructure.ExternalServices.TokenProviders
{
    public class PasswordResetTokenProvider : DataProtectorTokenProvider<User>
    {
        public PasswordResetTokenProvider(IDataProtectionProvider dataProtectionProvider, 
            IOptions<PasswordResetTokenProviderOptions> options, 
            ILogger<DataProtectorTokenProvider<User>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }
}
