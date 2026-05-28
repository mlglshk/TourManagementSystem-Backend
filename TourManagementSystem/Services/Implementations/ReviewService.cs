using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(ApplicationDbContext context, ILogger<ReviewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReviewResponseDto> CreateReviewAsync(int userId, ReviewCreateDto createDto)
        {
            // 1. Проверяем, существует ли тур
            var tour = await _context.Tours.FindAsync(createDto.TourId);
            if (tour == null)
                throw new Exception("Тур не найден");

            // 2. Проверяем, не оставлял ли пользователь уже отзыв на этот тур
            var existingReview = await _context.Set<Review>()
                .FirstOrDefaultAsync(r => r.TourId == createDto.TourId && r.UserId == userId);

            if (existingReview != null)
                throw new Exception("Вы уже оставляли отзыв на этот тур");

            // 3. Проверяем, есть ли у пользователя подтвержденное бронирование этого тура
            var hasCompletedBooking = await _context.Bookings
                .AnyAsync(b => b.UserId == userId &&
                              b.TourSchedule != null &&
                              b.TourSchedule.TourId == createDto.TourId &&
                              b.Status == "Confirmed");

            // 4. Создаем отзыв
            var review = new Review
            {
                TourId = createDto.TourId,
                UserId = userId,
                Rating = createDto.Rating,
                Comment = createDto.Comment,
                CreatedAt = DateTime.UtcNow,
                IsVerified = hasCompletedBooking // Автоматическая верификация, если есть бронь
            };

            _context.Set<Review>().Add(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Пользователь {userId} оставил отзыв на тур {createDto.TourId}");

            return await GetReviewByIdAsync(review.Id);
        }

        public async Task<ReviewResponseDto> GetReviewByIdAsync(int id)
        {
            var review = await _context.Set<Review>()
                .Include(r => r.User)
                .Include(r => r.Tour)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
                throw new Exception("Отзыв не найден");

            return MapToDto(review);
        }

        public async Task<ReviewResponseDto> UpdateReviewAsync(int id, int userId, bool isAdmin, ReviewUpdateDto updateDto)
        {
            var review = await _context.Set<Review>()
                .Include(r => r.User)
                .Include(r => r.Tour)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
                throw new Exception("Отзыв не найден");

            // Проверка прав: только автор или админ
            if (review.UserId != userId && !isAdmin)
                throw new UnauthorizedAccessException("Вы можете редактировать только свои отзывы");

            if (updateDto.Rating.HasValue)
                review.Rating = updateDto.Rating.Value;

            if (updateDto.Comment != null)
                review.Comment = updateDto.Comment;

            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Отзыв {id} обновлен пользователем {userId}");

            return MapToDto(review);
        }

        public async Task<bool> DeleteReviewAsync(int id, int userId, bool isAdmin)
        {
            var review = await _context.Set<Review>().FindAsync(id);

            if (review == null)
                return false;

            // Проверка прав: только автор или админ
            if (review.UserId != userId && !isAdmin)
                throw new UnauthorizedAccessException("Вы можете удалять только свои отзывы");

            _context.Set<Review>().Remove(review);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Отзыв {id} удален пользователем {userId}");

            return true;
        }

        public async Task<List<ReviewResponseDto>> GetReviewsByTourAsync(int tourId)
        {
            var reviews = await _context.Set<Review>()
                .Include(r => r.User)
                .Include(r => r.Tour)
                .Where(r => r.TourId == tourId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(MapToDto).ToList();
        }

        public async Task<List<ReviewResponseDto>> GetReviewsByUserAsync(int userId)
        {
            var reviews = await _context.Set<Review>()
                .Include(r => r.User)
                .Include(r => r.Tour)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(MapToDto).ToList();
        }

        public async Task<List<ReviewResponseDto>> GetAllReviewsAsync()
        {
            var reviews = await _context.Set<Review>()
                .Include(r => r.User)
                .Include(r => r.Tour)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(MapToDto).ToList();
        }

        public async Task<TourRatingSummaryDto> GetTourRatingSummaryAsync(int tourId)
        {
            var tour = await _context.Tours.FindAsync(tourId);
            if (tour == null)
                throw new Exception("Тур не найден");

            var reviews = await _context.Set<Review>()
                .Where(r => r.TourId == tourId)
                .ToListAsync();

            var distribution = new Dictionary<int, int>
            {
                { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
            };

            foreach (var review in reviews)
            {
                if (distribution.ContainsKey(review.Rating))
                    distribution[review.Rating]++;
            }

            return new TourRatingSummaryDto
            {
                TourId = tourId,
                TourTitle = tour.Title,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                TotalReviews = reviews.Count,
                RatingDistribution = distribution
            };
        }

        public async Task<double> GetAverageRatingForTourAsync(int tourId)
        {
            var summary = await GetTourRatingSummaryAsync(tourId);
            return summary.AverageRating;
        }

        public async Task<bool> VerifyReviewAsync(int id, bool isVerified)
        {
            var review = await _context.Set<Review>().FindAsync(id);
            if (review == null)
                return false;

            review.IsVerified = isVerified;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Отзыв {id} {(isVerified ? "верифицирован" : "деверифицирован")}");
            return true;
        }

        private ReviewResponseDto MapToDto(Review review)
        {
            return new ReviewResponseDto
            {
                Id = review.Id,
                TourId = review.TourId,
                UserId = review.UserId,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
                IsVerified = review.IsVerified,
                UserName = review.User != null ? $"{review.User.FirstName} {review.User.LastName}" : null,
                TourTitle = review.Tour?.Title
            };
        }
    }
}