using ApexBooking.Core.Domain.Services;
using Microsoft.AspNetCore.DataProtection;

namespace ApexBooking.Infrastructure.ExternalServices.DataProtection;

public sealed class DataProtectionSecretProtector : ISecretProtector
{
    // Distinct purpose string isolates this key derivation from every other Data Protection
    // consumer in the app (e.g. Identity's PasswordResetTokenProvider/EmailVerificationTokenProvider,
    // which already use the ambient IDataProtectionProvider under their own purposes) — a ciphertext
    // produced under one purpose cannot be unprotected under another.
    private const string Purpose = "ApexBooking.TenantPaymentCredentials.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
