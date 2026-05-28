using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class TourScheduleUpdateDto
    {
        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Range(0, 1000)]
        public int? AvailableSlots { get; set; }

        [Range(0, 100000)]
        public decimal? Price { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        public string? Notes { get; set; }
    }
}