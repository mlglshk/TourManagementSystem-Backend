using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class BasicPaymentService : IPaymentProcessor
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BasicPaymentService> _logger;

        public BasicPaymentService(
            ApplicationDbContext context,
            ILogger<BasicPaymentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1. Валидация данных карты
        public PaymentValidationResult ValidatePaymentData(PaymentCreateDto paymentDto)
        {
            var result = new PaymentValidationResult { IsValid = true };

            // Проверка номера карты (алгоритм Луна)
            if (!IsValidCardNumber(paymentDto.CardNumber))
            {
                result.IsValid = false;
                result.Errors.Add("Неверный номер карты");
            }

            // Проверка срока действия
            if (!IsValidExpiryDate(paymentDto.CardExpiry))
            {
                result.IsValid = false;
                result.Errors.Add("Неверная дата истечения срока или карта просрочена");
            }

            // Проверка CVV
            if (paymentDto.CardCvv.Length < 3 || paymentDto.CardCvv.Length > 4 || !paymentDto.CardCvv.All(char.IsDigit))
            {
                result.IsValid = false;
                result.Errors.Add("Неверный CVV код");
            }

            // Проверка имени держателя
            if (string.IsNullOrWhiteSpace(paymentDto.CardHolderName) || paymentDto.CardHolderName.Length < 2)
            {
                result.IsValid = false;
                result.Errors.Add("Неверное имя держателя карты");
            }

            return result;
        }

        // 2. Алгоритм Луна для проверки номера карты
        private bool IsValidCardNumber(string cardNumber)
        {
            try
            {
                // Убираем пробелы и дефисы
                cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

                if (cardNumber.Length < 13 || cardNumber.Length > 19)
                    return false;

                int sum = 0;
                bool alternate = false;

                for (int i = cardNumber.Length - 1; i >= 0; i--)
                {
                    if (!char.IsDigit(cardNumber[i]))
                        return false;

                    int digit = int.Parse(cardNumber[i].ToString());

                    if (alternate)
                    {
                        digit *= 2;
                        if (digit > 9)
                            digit -= 9;
                    }

                    sum += digit;
                    alternate = !alternate;
                }

                return sum % 10 == 0;
            }
            catch
            {
                return false;
            }
        }

        // 3. Проверка срока действия карты
        private bool IsValidExpiryDate(string expiry)
        {
            try
            {
                if (expiry.Length != 5 || expiry[2] != '/')
                    return false;

                var parts = expiry.Split('/');
                if (parts.Length != 2 || parts[0].Length != 2 || parts[1].Length != 2)
                    return false;

                if (!int.TryParse(parts[0], out int month) ||
                    !int.TryParse(parts[1], out int year))
                    return false;

                if (month < 1 || month > 12)
                    return false;

                // Добавляем 2000 для года (формат YY)
                int fullYear = 2000 + year;
                var expiryDate = new DateTime(fullYear, month, 1).AddMonths(1).AddDays(-1);

                return expiryDate >= DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        // 4. Симуляция обработки платежа
        public async Task<PaymentResultDto> ProcessPaymentAsync(PaymentCreateDto paymentDto, decimal amount)
        {
            _logger.LogInformation($"Начало обработки платежа на сумму: {amount}");

            try
            {
                // Валидация данных
                var validationResult = ValidatePaymentData(paymentDto);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning($"Невалидные данные платежа: {string.Join(", ", validationResult.Errors)}");
                    return new PaymentResultDto
                    {
                        Success = false,
                        ErrorMessage = $"Ошибка валидации: {string.Join(", ", validationResult.Errors)}",
                        ProcessedAt = DateTime.UtcNow
                    };
                }

                // Имитация задержки сети
                await Task.Delay(1000);

                // Генерация transactionId
                string transactionId = $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

                // Симуляция успешного/неуспешного платежа
                // 80% успешных, 20% неуспешных
                bool isSuccess = new Random().Next(100) < 80;

                if (isSuccess)
                {
                    _logger.LogInformation($"Платеж успешен. TransactionId: {transactionId}");
                    return new PaymentResultDto
                    {
                        Success = true,
                        TransactionId = transactionId,
                        ProcessedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    // Симуляция различных ошибок
                    var errors = new[]
                    {
                        "Недостаточно средств на карте",
                        "Карта заблокирована",
                        "Превышен лимит операции",
                        "Банк отклонил операцию"
                    };

                    string error = errors[new Random().Next(errors.Length)];

                    _logger.LogWarning($"Платеж отклонен: {error}. TransactionId: {transactionId}");
                    return new PaymentResultDto
                    {
                        Success = false,
                        TransactionId = transactionId,
                        ErrorMessage = error,
                        ProcessedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки платежа");
                return new PaymentResultDto
                {
                    Success = false,
                    ErrorMessage = "Внутренняя ошибка платежной системы",
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }

        // 5. Запись в историю платежей
        public async Task LogPaymentHistoryAsync(int paymentId, string status, string? notes = null)
        {
            try
            {
                var history = new PaymentHistory
                {
                    PaymentId = paymentId,
                    Status = status,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentHistories.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogDebug($"Запись в историю платежей: PaymentId={paymentId}, Status={status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка записи в историю платежей для PaymentId={paymentId}");
            }
        }
    }

    // Вспомогательные классы
   

    
}