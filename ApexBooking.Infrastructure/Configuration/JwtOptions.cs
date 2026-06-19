using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Infrastructure.Configuration
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public int AccessTokenExpiryMinutes { get; set; }
        public string PrivateKeyPem { get; set; } = default!;
        public string PublicKeyPem { get; set; } = default!;
    }
}