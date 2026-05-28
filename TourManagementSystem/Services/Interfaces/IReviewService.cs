using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface IReviewService
    {
        // Основные CRUD
        Task<ReviewResponseDto> GetReviewByIdAsync(int id);
        Task<ReviewResponseDto> CreateReviewAsync(int userId, ReviewCreateDto createDto);
        Task<ReviewResponseDto> UpdateReviewAsync(int id, int userId, bool isAdmin, ReviewUpdateDto updateDto);
        Task<bool> DeleteReviewAsync(int id, int userId, bool isAdmin);

        // Получение отзывов
        Task<List<ReviewResponseDto>> GetReviewsByTourAsync(int tourId);
        Task<List<ReviewResponseDto>> GetReviewsByUserAsync(int userId);
        Task<List<ReviewResponseDto>> GetAllReviewsAsync(); // только для админов

        // Статистика и рейтинги
        Task<TourRatingSummaryDto> GetTourRatingSummaryAsync(int tourId);
        Task<double> GetAverageRatingForTourAsync(int tourId);

        // Верификация отзывов (только админ)
        Task<bool> VerifyReviewAsync(int id, bool isVerified);
    }
}