using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class FavoriteService : IFavoriteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITourService _tourService;

        public FavoriteService(ApplicationDbContext context, ITourService tourService)
        {
            _context = context;
            _tourService = tourService;
        }

        // 1. Добавить в избранное
        public async Task<bool> AddToFavoritesAsync(int userId, int tourId)
        {
            try
            {
                // Проверяем существование тура
                var tour = await _tourService.GetTourByIdAsync(tourId);

                // Проверяем, не добавлен ли уже
                var exists = await _context.FavoriteTours
                    .AnyAsync(f => f.UserId == userId && f.TourId == tourId);

                if (exists) return false; // Уже в избранном

                // Добавляем
                var favorite = new FavoriteTour
                {
                    UserId = userId,
                    TourId = tourId,
                    AddedAt = DateTime.UtcNow
                };

                _context.FavoriteTours.Add(favorite);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 2. Удалить из избранного
        public async Task<bool> RemoveFromFavoritesAsync(int userId, int tourId)
        {
            var favorite = await _context.FavoriteTours
                .FirstOrDefaultAsync(f => f.UserId == userId && f.TourId == tourId);

            if (favorite == null) return false;

            _context.FavoriteTours.Remove(favorite);
            await _context.SaveChangesAsync();

            return true;
        }

        // 3. Проверить наличие в избранном
        public async Task<bool> IsTourInFavoritesAsync(int userId, int tourId)
        {
            return await _context.FavoriteTours
                .AnyAsync(f => f.UserId == userId && f.TourId == tourId);
        }

        // 4. Получить все избранные туры
        public async Task<List<TourResponseDto>> GetUserFavoritesAsync(int userId)
        {
            // Получаем ID избранных туров
            var favoriteTourIds = await _context.FavoriteTours
                .Where(f => f.UserId == userId)
                .Select(f => f.TourId)
                .ToListAsync();

            if (!favoriteTourIds.Any()) return new List<TourResponseDto>();

            // Получаем детали каждого тура
            var favorites = new List<TourResponseDto>();
            foreach (var tourId in favoriteTourIds)
            {
                try
                {
                    var tour = await _tourService.GetTourByIdAsync(tourId);
                    favorites.Add(tour);
                }
                catch
                {
                    // Тур мог быть удален - пропускаем
                }
            }

            return favorites.OrderByDescending(t => t.Id).ToList();
        }
    }
}