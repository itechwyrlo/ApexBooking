namespace ApexBooking.Core.Domain.Services;

/// <summary>
/// Envelope-encrypts secrets at rest — currently TenantPaymentCredential.SecretKey/WebhookSecret.
/// Backed by ASP.NET Core Data Protection (AES-256-CBC + HMAC authenticated encryption); see
/// DataProtectionSecretProtector for the concrete implementation and key-ring storage.
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string ciphertext);
}
