using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourManagementSystem.Models
{
    [Table("tours")]
    public class Tour
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        public int? DurationHours { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal BasePrice { get; set; }

        public int? MaxParticipants { get; set; }

        [MaxLength(20)]
        public string? DifficultyLevel { get; set; } // Easy, Medium, Hard

        [MaxLength(50)]
        public string? Category { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public virtual ICollection<TourSchedule>? Schedules { get; set; }
        public virtual ICollection<TourImage>? Images { get; set; } // ✅ ДОБАВЛЕНО
    }
}