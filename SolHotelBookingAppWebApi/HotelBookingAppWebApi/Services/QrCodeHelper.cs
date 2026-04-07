using QRCoder;

namespace HotelBookingAppWebApi.Services
{
    /// <summary>
    /// Generates QR code images as Base64-encoded PNG strings.
    /// Used for UPI payment QR codes in the reservation flow.
    /// </summary>
    public static class QrCodeHelper
    {
        private const int PixelsPerModule = 10;

        /// <summary>
        /// Encodes <paramref name="content"/> as a QR code and returns it as a Base64 PNG string.
        /// </summary>
        public static string GenerateQrCodeBase64(string content)
        {
            var pngBytes = RenderQrCodePng(content);
            return Convert.ToBase64String(pngBytes);
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────────────────

        private static byte[] RenderQrCodePng(string content)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var code = new PngByteQRCode(data);
            return code.GetGraphic(PixelsPerModule);
        }
    }
}
