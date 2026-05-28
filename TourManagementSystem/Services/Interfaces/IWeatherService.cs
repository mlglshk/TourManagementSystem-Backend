using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<WeatherResponseDto> GetWeatherForecastAsync(string city, DateTime date);
        Task<WeatherResponseDto> GetWeatherForecastByCoordinatesAsync(double lat, double lon, DateTime date);
        Task<CurrentWeatherResponseDto> GetCurrentWeatherAsync(string city);
        Task<WeatherResponseDto> GetWeatherForTourScheduleAsync(int scheduleId);
    }
}