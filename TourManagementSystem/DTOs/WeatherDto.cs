namespace TourManagementSystem.DTOs
{
    public class WeatherRequestDto
    {
        public string City { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
    }

    public class WeatherResponseDto
    {
        public string Location { get; set; } = string.Empty;
        public DateTime ForecastDate { get; set; }
        public decimal? TemperatureMin { get; set; }
        public decimal? TemperatureMax { get; set; }
        public string? Condition { get; set; }
        public int? Humidity { get; set; }
        public decimal? WindSpeed { get; set; }
        public string? Icon { get; set; }
        public string? Recommendation { get; set; } // Рекомендация по одежде
        public bool IsCached { get; set; }
    }

    public class CurrentWeatherResponseDto
    {
        public string Location { get; set; } = string.Empty;
        public DateTime ObservedAt { get; set; }
        public decimal Temperature { get; set; }
        public string Condition { get; set; } = string.Empty;
        public int Humidity { get; set; }
        public decimal WindSpeed { get; set; }
        public string? Icon { get; set; }
    }

    // DTO
    public class BatchWeatherRequestDto
    {
        public List<WeatherItemDto> Items { get; set; } = new();
    }

    public class WeatherItemDto
    {
        public string City { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}