using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class TourImageService : ITourImageService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TourImageService> _logger;

        public TourImageService(ApplicationDbContext context, ILogger<TourImageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TourImageDto>> GetImagesByTourAsync(int tourId)
        {
            var images = await _context.TourImages
                .Where(i => i.TourId == tourId)
                .OrderBy(i => i.OrderIndex)
                .ToListAsync();

            return images.Select(MapToDto).ToList();
        }

        public async Task<TourImageDto> AddImageAsync(int tourId, TourImageCreateDto createDto)
        {
            // Проверяем существование тура
            var tour = await _context.Tours.FindAsync(tourId);
            if (tour == null)
                throw new Exception("Тур не найден");

            // Получаем следующий порядковый номер
            var maxOrder = await _context.TourImages
                .Where(i => i.TourId == tourId)
                .MaxAsync(i => (int?)i.OrderIndex) ?? -1;

            var image = new TourImage
            {
                TourId = tourId,
                ImageUrl = createDto.ImageUrl,
                AltText = createDto.AltText,
                IsPrimary = createDto.IsPrimary,
                OrderIndex = maxOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            // Если это главное фото, снимаем флаг с других фото этого тура
            if (image.IsPrimary)
            {
                await RemovePrimaryFlagAsync(tourId);
            }

            _context.TourImages.Add(image);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Добавлено фото к туру {tourId}: {image.ImageUrl}");

            return MapToDto(image);
        }

        public async Task<TourImageDto> UpdateImageAsync(int imageId, TourImageUpdateDto updateDto)
        {
            var image = await _context.TourImages.FindAsync(imageId);
            if (image == null)
                throw new Exception("Фото не найдено");

            if (updateDto.ImageUrl != null)
                image.ImageUrl = updateDto.ImageUrl;

            if (updateDto.AltText != null)
                image.AltText = updateDto.AltText;

            if (updateDto.OrderIndex.HasValue)
                image.OrderIndex = updateDto.OrderIndex.Value;

            if (updateDto.IsPrimary.HasValue && updateDto.IsPrimary.Value && !image.IsPrimary)
            {
                await RemovePrimaryFlagAsync(image.TourId);
                image.IsPrimary = true;
            }
            else if (updateDto.IsPrimary.HasValue && !updateDto.IsPrimary.Value)
            {
                image.IsPrimary = false;
            }

            await _context.SaveChangesAsync();

            return MapToDto(image);
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            var image = await _context.TourImages.FindAsync(imageId);
            if (image == null)
                return false;

            _context.TourImages.Remove(image);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Удалено фото {imageId}");

            return true;
        }

        public async Task<TourImageDto> SetPrimaryImageAsync(int tourId, int imageId)
        {
            // Проверяем, что фото принадлежит этому туру
            var image = await _context.TourImages
                .FirstOrDefaultAsync(i => i.Id == imageId && i.TourId == tourId);

            if (image == null)
                throw new Exception("Фото не найдено в галерее этого тура");

            // Снимаем флаг primary со всех фото тура
            await RemovePrimaryFlagAsync(tourId);

            // Устанавливаем новое главное фото
            image.IsPrimary = true;
            await _context.SaveChangesAsync();

            return MapToDto(image);
        }

        // Вспомогательный метод: снять флаг "главное" со всех фото тура
        private async Task RemovePrimaryFlagAsync(int tourId)
        {
            var currentPrimary = await _context.TourImages
                .FirstOrDefaultAsync(i => i.TourId == tourId && i.IsPrimary);

            if (currentPrimary != null)
            {
                currentPrimary.IsPrimary = false;
            }
        }

        // Маппинг модели в DTO (используем существующий TourImageDto)
        private TourImageDto MapToDto(TourImage image)
        {
            return new TourImageDto
            {
                Id = image.Id,
                ImageUrl = image.ImageUrl,
                AltText = image.AltText,
                IsPrimary = image.IsPrimary,
                OrderIndex = image.OrderIndex,
                CreatedAt = image.CreatedAt
            };
        }
    }
}