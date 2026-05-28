using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourManagementSystem.Models
{
    [Table("weather_cache")]
    public class WeatherCache
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public DateOnly ForecastDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? TemperatureMin { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? TemperatureMax { get; set; }

        [MaxLength(100)]
        public string? Condition { get; set; }

        public int? Humidity { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? WindSpeed { get; set; }

        [MaxLength(50)]
        public string? Icon { get; set; }

        public DateTime CachedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }
    }
}