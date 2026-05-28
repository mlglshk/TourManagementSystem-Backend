using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface IFavoriteService
    {
        // 1. Добавить тур в избранное
        Task<bool> AddToFavoritesAsync(int userId, int tourId);

        // 2. Удалить тур из избранного
        Task<bool> RemoveFromFavoritesAsync(int userId, int tourId);

        // 3. Проверить, есть ли тур в избранном
        Task<bool> IsTourInFavoritesAsync(int userId, int tourId);

        // 4. Получить все избранные туры пользователя
        Task<List<TourResponseDto>> GetUserFavoritesAsync(int userId);
    }
}