using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class ReviewCreateDto
    {
        [Required]
        public int TourId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }
    }

    public class ReviewUpdateDto
    {
        [Range(1, 5)]
        public int? Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }
    }

    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int TourId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsVerified { get; set; }

        // Дополнительная информация для отображения
        public string? UserName { get; set; }
        public string? TourTitle { get; set; }
    }

    public class TourRatingSummaryDto
    {
        public int TourId { get; set; }
        public string TourTitle { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new(); // 1⭐,2⭐,3⭐,4⭐,5⭐
    }
}