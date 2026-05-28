namespace TourManagementSystem.DTOs
{
    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
        // Эти поля нужны для ЮKassa и QR-кода
        public string? ConfirmationUrl { get; set; }  // Для редиректа на ЮKassa
        public string? QrCodeUrl { get; set; }       // Для QR-кода СБП
        public string? Note { get; set; }            // Дополнительная информация
        public string? Provider { get; set; }        // Какой провайдер использовался
    }

    public class PaymentHistoryDto
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}