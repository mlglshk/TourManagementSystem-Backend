using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class BookingUpdateDto
    {
        [Range(1, 100)]
        public int? Participants { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        public string? SpecialRequirements { get; set; }

        public string? CancellationReason { get; set; }
    }
}