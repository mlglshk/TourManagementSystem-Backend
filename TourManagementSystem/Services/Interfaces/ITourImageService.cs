using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface ITourImageService
    {
        // Получить все фото тура
        Task<List<TourImageDto>> GetImagesByTourAsync(int tourId);

        // Добавить новое фото
        Task<TourImageDto> AddImageAsync(int tourId, TourImageCreateDto createDto);

        // Обновить фото
        Task<TourImageDto> UpdateImageAsync(int imageId, TourImageUpdateDto updateDto);

        // Удалить фото
        Task<bool> DeleteImageAsync(int imageId);

        // Сделать фото главным
        Task<TourImageDto> SetPrimaryImageAsync(int tourId, int imageId);
    }
}