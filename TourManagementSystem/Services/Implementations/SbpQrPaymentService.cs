using TourManagementSystem.DTOs;
using TourManagementSystem.Helpers;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class SbpQrPaymentService : IPaymentProcessor
    {
        private readonly ILogger<SbpQrPaymentService> _logger;
        private readonly IWebHostEnvironment _environment;

        public SbpQrPaymentService(ILogger<SbpQrPaymentService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public PaymentValidationResult ValidatePaymentData(PaymentCreateDto paymentDto)
        {
            // Для QR-кода данные карты не нужны
            return new PaymentValidationResult { IsValid = true };
        }

        public async Task<PaymentResultDto> ProcessPaymentAsync(PaymentCreateDto paymentDto, decimal amount)
        {
            _logger.LogInformation($"Генерация QR-кода для оплаты {amount} руб.");

            var paymentId = Guid.NewGuid().ToString();
            var payload = $"Оплата тура #{paymentDto.BookingId}|Сумма:{amount}|ID:{paymentId}";

            // Генерируем QR-код
            var qrImage = QrCodeHelper.GeneratePng(payload);
            var qrCodeUrl = await SaveQrCode(qrImage, paymentId);

            return new PaymentResultDto
            {
                Success = true,
                TransactionId = paymentId,
                ProcessedAt = DateTime.UtcNow,
                Provider = "SbpQR",
                QrCodeUrl = qrCodeUrl,
                Note = "Отсканируйте QR-код в приложении любого банка для оплаты"
            };
        }

        private async Task<string> SaveQrCode(byte[] qrImage, string paymentId)
        {
            var directory = Path.Combine(_environment.WebRootPath, "qrcodes");
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var fileName = $"qr_{paymentId}.png";
            var filePath = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(filePath, qrImage);

            return $"/qrcodes/{fileName}";
        }

        public Task LogPaymentHistoryAsync(int paymentId, string status, string? notes = null)
        {
            return Task.CompletedTask;
        }
    }
}