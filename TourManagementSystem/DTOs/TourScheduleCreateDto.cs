using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class TourScheduleCreateDto
    {
        [Required]
        public int TourId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [Range(1, 1000)]
        public int AvailableSlots { get; set; }

        [Required]
        [Range(0, 100000)]
        public decimal Price { get; set; }

        public string? Notes { get; set; }
    }
}