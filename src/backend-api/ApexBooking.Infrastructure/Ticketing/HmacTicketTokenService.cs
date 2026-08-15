using System;
using System.Security.Cryptography;
using System.Text;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Infrastructure.Ticketing
{
    public sealed class HmacTicketTokenService : ITicketTokenService
    {
        private const int SignatureBytes = 16; // truncated HMAC — 128-bit forgery resistance
        private const int PayloadBytes = 48;   // BookingId + TenantId + BranchId, 16 bytes each

        private readonly byte[] _key;

        public HmacTicketTokenService(IOptions<SecurityOptions> options)
        {
            var secret = options.Value.TicketSigningKey;
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
                throw new InvalidOperationException("Security:TicketSigningKey must be at least 32 characters.");

            _key = Encoding.UTF8.GetBytes(secret);
        }

        public string Issue(TicketPayload payload)
        {
            var body = Pack(payload);
            var signature = HMACSHA256.HashData(_key, body).AsSpan(0, SignatureBytes).ToArray();
            return $"{Base64Url(body)}.{Base64Url(signature)}";
        }

        public bool TryValidate(string token, out TicketPayload payload)
        {
            payload = default;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var separatorIndex = token.IndexOf('.');
            if (separatorIndex <= 0 || separatorIndex == token.Length - 1)
                return false;

            byte[] body;
            byte[] signature;
            try
            {
                body = FromBase64Url(token[..separatorIndex]);
                signature = FromBase64Url(token[(separatorIndex + 1)..]);
            }
            catch (FormatException)
            {
                return false;
            }

            if (body.Length != PayloadBytes || signature.Length != SignatureBytes)
                return false;

            var expectedSignature = HMACSHA256.HashData(_key, body).AsSpan(0, SignatureBytes).ToArray();

            // Fixed-time comparison blocks a timing-attack oracle against the signature bytes.
            if (!CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
                return false;

            payload = Unpack(body);
            return true;
        }

        private static byte[] Pack(TicketPayload payload)
        {
            var buffer = new byte[PayloadBytes];
            payload.BookingId.Value.TryWriteBytes(buffer.AsSpan(0, 16));
            payload.TenantId.Value.TryWriteBytes(buffer.AsSpan(16, 16));
            payload.BranchId.Value.TryWriteBytes(buffer.AsSpan(32, 16));
            return buffer;
        }

        private static TicketPayload Unpack(ReadOnlySpan<byte> body) => new(
            new BookingId(new Guid(body[..16])),
            new TenantId(new Guid(body[16..32])),
            new BranchId(new Guid(body[32..48])));

        private static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] FromBase64Url(string value)
        {
            var normalized = value.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(normalized.PadRight((normalized.Length + 3) & ~3, '='));
        }
    }
}
