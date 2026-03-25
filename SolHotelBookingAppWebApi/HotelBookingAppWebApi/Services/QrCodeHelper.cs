using QRCoder;

namespace HotelBookingAppWebApi.Services
{
    public static class QrCodeHelper
    {
        public static string GenerateQrCodeBase64(string upiString)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(upiString, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var bytes = qrCode.GetGraphic(10);
            return Convert.ToBase64String(bytes);
        }
    }
}
