using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class YooKassaPaymentService : IPaymentProcessor
    {
        private readonly ILogger<YooKassaPaymentService> _logger;
        private readonly string _shopId;
        private readonly string _secretKey;
        private readonly bool _isTestMode;

        public YooKassaPaymentService(IConfiguration configuration, ILogger<YooKassaPaymentService> logger)
        {
            _logger = logger;
            _shopId = configuration["Payment:YooKassa:ShopId"] ?? "";
            _secretKey = configuration["Payment:YooKassa:SecretKey"] ?? "";
            _isTestMode = configuration.GetValue<bool>("Payment:YooKassa:IsTestMode", true);
        }

        public PaymentValidationResult ValidatePaymentData(PaymentCreateDto paymentDto)
        {
            // ЮKassa не требует валидации на бэкенде
            return new PaymentValidationResult { IsValid = true };
        }

        public async Task<PaymentResultDto> ProcessPaymentAsync(PaymentCreateDto paymentDto, decimal amount)
        {
            _logger.LogInformation($"ЮKassa: попытка оплаты {amount} руб. для бронирования {paymentDto.BookingId}");

            // Проверка настроек
            if (string.IsNullOrEmpty(_shopId) || string.IsNullOrEmpty(_secretKey))
            {
                return new PaymentResultDto
                {
                    Success = false,
                    ErrorMessage = "ЮKassa не настроена. Укажите ShopId и SecretKey в настройках.",
                    ProcessedAt = DateTime.UtcNow,
                    Provider = "YooKassa"
                };
            }

            // TODO: Здесь будет реальный вызов API ЮKassa
            // Пока возвращаем заглушку для демонстрации
            await Task.Delay(500);

            return new PaymentResultDto
            {
                Success = true,
                TransactionId = $"YK_{DateTime.Now:yyyyMMddHHmmss}_{new Random().Next(1000, 9999)}",
                ProcessedAt = DateTime.UtcNow,
                Provider = "YooKassa",
                ConfirmationUrl = _isTestMode
                    ? "https://demomoney.yookassa.ru/payment/test-success"
                    : "https://yookassa.ru/payment/success",
                Note = _isTestMode
                    ? "ТЕСТОВЫЙ РЕЖИМ: платеж не списывает реальные деньги"
                    : "Платеж обрабатывается"
            };
        }

        public Task LogPaymentHistoryAsync(int paymentId, string status, string? notes = null)
        {
            return Task.CompletedTask;
        }
    }
}