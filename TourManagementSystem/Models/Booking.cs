using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourManagementSystem.Models
{
    [Table("bookings")]
    public class Booking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string BookingNumber { get; set; } = string.Empty;

        [Required]
        public int? UserId { get; set; }

        [Required]
        public int TourScheduleId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Participants { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled, Completed

        public string? SpecialRequirements { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? CancellationReason { get; set; }

        // Навигационные свойства
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("TourScheduleId")]
        public virtual TourSchedule? TourSchedule { get; set; }

        public virtual ICollection<Payment>? Payments { get; set; }
    }
}