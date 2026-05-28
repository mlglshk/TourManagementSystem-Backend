using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class PaymentCreateDto
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Card";

        [MaxLength(100)]
        public string? TransactionId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // ✅ ДОБАВИМ ДАННЫЕ КАРТЫ (для симуляции)
        [Required]
        [CreditCard]
        [MaxLength(19)]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Формат MM/YY")]
        public string CardExpiry { get; set; } = string.Empty;

        [Required]
        [StringLength(4, MinimumLength = 3, ErrorMessage = "CVV должен быть 3-4 цифры")]
        public string CardCvv { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CardHolderName { get; set; } = string.Empty;
    }
}