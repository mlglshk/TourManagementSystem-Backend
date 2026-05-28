using QRCoder;

namespace TourManagementSystem.Helpers
{
    public static class QrCodeHelper
    {
        public static byte[] GeneratePng(string text, int pixelsPerModule = 20)
        {
            using var generator = new QRCodeGenerator();
            var qrData = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            return qrCode.GetGraphic(pixelsPerModule);
        }
    }
}