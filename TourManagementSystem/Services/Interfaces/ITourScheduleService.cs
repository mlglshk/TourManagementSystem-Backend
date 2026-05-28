using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface ITourScheduleService
    {
        Task<List<TourScheduleResponseDto>> GetAllSchedulesAsync();
        Task<TourScheduleResponseDto> GetScheduleByIdAsync(int id);
        Task<List<TourScheduleResponseDto>> GetSchedulesByTourAsync(int tourId);
        Task<List<TourScheduleResponseDto>> GetAvailableSchedulesAsync();
        Task<List<TourScheduleSimpleDto>> GetSchedulesForDropdownAsync();
        Task<TourScheduleResponseDto> UpdateScheduleAsync(int id, TourScheduleUpdateDto updateDto);
        Task<bool> DeleteScheduleAsync(int id);
        Task<TourScheduleResponseDto> CreateScheduleAsync(TourScheduleCreateDto createDto);

    }
}