using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class BookingCreateDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int TourScheduleId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Participants { get; set; }

        public string? SpecialRequirements { get; set; }
    }
}