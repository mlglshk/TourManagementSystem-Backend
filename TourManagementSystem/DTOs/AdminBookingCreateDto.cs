using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class AdminBookingCreateDto
    {
        [Required]
        public int TourScheduleId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Participants { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Введите корректный email")]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? UserPhone { get; set; }

        public string? SpecialRequirements { get; set; }
    }
}