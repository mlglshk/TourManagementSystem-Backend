using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class TourUpdateDto
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        public int? DurationHours { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? BasePrice { get; set; }

        public int? MaxParticipants { get; set; }

        [MaxLength(20)]
        public string? DifficultyLevel { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        public bool? IsActive { get; set; }
    }
}