using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class PaymentUpdateDto
    {
        [Range(0.01, 100000)]
        public decimal? Amount { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        [MaxLength(100)]
        public string? TransactionId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}