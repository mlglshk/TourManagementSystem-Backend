using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourManagementSystem.Models
{
    [Table("favorite_tours")]
    public class FavoriteTour
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int TourId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Навигационные свойства (опционально, для EF)
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("TourId")]
        public virtual Tour? Tour { get; set; }
    }
}