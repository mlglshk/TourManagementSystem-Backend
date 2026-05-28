using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface ITourService
    {
        Task<List<TourResponseDto>> GetAllToursAsync();
        Task<TourResponseDto> GetTourByIdAsync(int id);
        Task<TourWithSchedulesDto> GetTourWithSchedulesAsync(int id); // ✅ ДОБАВЛЕНО
        Task<TourResponseDto> CreateTourAsync(TourCreateDto createDto);
        Task<TourResponseDto> UpdateTourAsync(int id, TourUpdateDto updateDto);
        Task<bool> DeleteTourAsync(int id);
        Task<bool> ToggleTourStatusAsync(int id, bool isActive);
        Task<List<TourResponseDto>> SearchToursAsync(string searchTerm);
        Task<List<TourResponseDto>> GetToursByCategoryAsync(string category);
        Task<List<TourResponseDto>> GetToursByDifficultyAsync(string difficulty);
        Task<List<TourResponseDto>> SearchToursAdvancedAsync(string? searchTerm, string? category, string? difficulty);
    }
}