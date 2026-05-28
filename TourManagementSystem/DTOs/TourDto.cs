using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class TourCreateDto
    {
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
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal BasePrice { get; set; }

        public int? MaxParticipants { get; set; }

        [MaxLength(20)]
        public string? DifficultyLevel { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }
    }

    public class TourResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public string? Location { get; set; }
        public int? DurationHours { get; set; }
        public decimal BasePrice { get; set; }
        public int? MaxParticipants { get; set; }
        public string? DifficultyLevel { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ✅ ДОБАВЛЕНО: Изображения тура
        public List<TourImageDto> Images { get; set; } = new List<TourImageDto>();

        // ✅ ИСПРАВЛЕНО: Только вычисляемое свойство без приватных полей
        public string? PrimaryImageUrl
        {
            get
            {
                var primaryImage = Images?.FirstOrDefault(img => img.IsPrimary);
                if (primaryImage != null && !string.IsNullOrEmpty(primaryImage.ImageUrl))
                {
                    // Формируем полный URL для клиента
                    return $"/images/{primaryImage.ImageUrl.TrimStart('/')}";
                }
                return null;
            }
        }
    }

    // ✅ УЛУЧШЕНО: DTO для изображений с полным URL
    public class TourImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        // ✅ ДОБАВЛЕНО: Полный URL для клиента (вычисляемое свойство)
        public string FullImageUrl => $"/images/{ImageUrl.TrimStart('/')}";

        public string? AltText { get; set; }
        public bool IsPrimary { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TourImageCreateDto
    {
        [Required]
        [Url(ErrorMessage = "Неверный формат URL. Пример: https://example.com/photo.jpg")]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? AltText { get; set; }

        public bool IsPrimary { get; set; } = false;
    }

    public class TourImageUpdateDto
    {
        [Url(ErrorMessage = "Неверный формат URL")]
        public string? ImageUrl { get; set; }

        [MaxLength(255)]
        public string? AltText { get; set; }

        public bool? IsPrimary { get; set; }

        public int? OrderIndex { get; set; }
    }
}