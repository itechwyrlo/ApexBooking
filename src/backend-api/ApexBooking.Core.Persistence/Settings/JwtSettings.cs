namespace ApexBooking.Core.Persistence.Settings
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public int AccessTokenExpiryMinutes { get; set; }
        public string PrivateKeyPem { get; set; } = default!;
        public string PublicKeyPem { get; set; } = default!;
        public int RefreshTokenExpirationDays { get; init; }
    }
}