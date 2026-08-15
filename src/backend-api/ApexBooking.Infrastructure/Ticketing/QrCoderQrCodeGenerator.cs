using ApexBooking.Core.Domain.Services.Ticketing;
using QRCoder;

namespace ApexBooking.Infrastructure.Ticketing
{
    public sealed class QrCoderQrCodeGenerator : IQrCodeGenerator
    {
        // 20px per module: legible both at the on-screen display size and printed on a receipt,
        // without producing an oversized PNG for the confirmation email.
        private const int PixelsPerModule = 20;

        public byte[] GeneratePng(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            var pngQrCode = new PngByteQRCode(qrCodeData);
            return pngQrCode.GetGraphic(PixelsPerModule);
        }
    }
}
