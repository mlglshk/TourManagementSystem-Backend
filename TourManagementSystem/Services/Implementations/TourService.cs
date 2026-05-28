using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class TourService : ITourService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITourScheduleService _tourScheduleService;

        public TourService(ApplicationDbContext context, ITourScheduleService tourScheduleService)
        {
            _context = context;
            _tourScheduleService = tourScheduleService;
        }

        // ✅ ДОБАВЛЕНО: Метод для получения тура с расписаниями
        public async Task<TourWithSchedulesDto> GetTourWithSchedulesAsync(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Images!)
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

            if (tour == null)
                throw new Exception("Тур не найден");

            var schedules = await _tourScheduleService.GetSchedulesByTourAsync(id);

            return new TourWithSchedulesDto
            {
                Tour = MapToDto(tour),
                Schedules = schedules
            };
        }

        public async Task<List<TourResponseDto>> GetAllToursAsync()
        {
            var tours = await _context.Tours
                .Where(t => t.IsActive)
                .Include(t => t.Images!)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return tours.Select(MapToDto).ToList();
        }

        public async Task<TourResponseDto> GetTourByIdAsync(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Images!)
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

            if (tour == null)
                throw new Exception("Тур не найден");

            return MapToDto(tour);
        }

        public async Task<TourResponseDto> CreateTourAsync(TourCreateDto createDto)
        {
            var tour = new Tour
            {
                Title = createDto.Title,
                Description = createDto.Description,
                ShortDescription = createDto.ShortDescription,
                Location = createDto.Location,
                DurationHours = createDto.DurationHours,
                BasePrice = createDto.BasePrice,
                MaxParticipants = createDto.MaxParticipants,
                DifficultyLevel = createDto.DifficultyLevel,
                Category = createDto.Category,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            return await GetTourByIdAsync(tour.Id);
        }

        public async Task<TourResponseDto> UpdateTourAsync(int id, TourUpdateDto updateDto)
        {
            var tour = await _context.Tours
                .Include(t => t.Images!)
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);

            if (tour == null)
                throw new Exception("Тур не найден");

            // Обновляем только переданные поля
            if (!string.IsNullOrEmpty(updateDto.Title))
                tour.Title = updateDto.Title;

            if (!string.IsNullOrEmpty(updateDto.Description))
                tour.Description = updateDto.Description;

            if (!string.IsNullOrEmpty(updateDto.ShortDescription))
                tour.ShortDescription = updateDto.ShortDescription;

            if (!string.IsNullOrEmpty(updateDto.Location))
                tour.Location = updateDto.Location;

            if (updateDto.DurationHours.HasValue)
                tour.DurationHours = updateDto.DurationHours.Value;

            if (updateDto.BasePrice.HasValue)
                tour.BasePrice = updateDto.BasePrice.Value;

            if (updateDto.MaxParticipants.HasValue)
                tour.MaxParticipants = updateDto.MaxParticipants.Value;

            if (!string.IsNullOrEmpty(updateDto.DifficultyLevel))
                tour.DifficultyLevel = updateDto.DifficultyLevel;

            if (!string.IsNullOrEmpty(updateDto.Category))
                tour.Category = updateDto.Category;

            if (updateDto.IsActive.HasValue)
                tour.IsActive = updateDto.IsActive.Value;

            tour.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(tour);
        }

        public async Task<bool> DeleteTourAsync(int id)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null)
                return false;

            // Мягкое удаление - деактивация
            tour.IsActive = false;
            tour.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleTourStatusAsync(int id, bool isActive)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null)
                return false;

            tour.IsActive = isActive;
            tour.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TourResponseDto>> SearchToursAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllToursAsync();

            var tours = await _context.Tours
                .Where(t => t.IsActive &&
                           (t.Title.Contains(searchTerm) ||
                            t.Description.Contains(searchTerm) ||
                            t.Location.Contains(searchTerm) ||
                            t.Category.Contains(searchTerm)))
                .Include(t => t.Images!)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return tours.Select(MapToDto).ToList();
        }

        public async Task<List<TourResponseDto>> GetToursByCategoryAsync(string category)
        {
            var tours = await _context.Tours
                .Where(t => t.IsActive && t.Category == category)
                .Include(t => t.Images!)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return tours.Select(MapToDto).ToList();
        }

        public async Task<List<TourResponseDto>> GetToursByDifficultyAsync(string difficulty)
        {
            var tours = await _context.Tours
                .Where(t => t.IsActive && t.DifficultyLevel == difficulty)
                .Include(t => t.Images!)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return tours.Select(MapToDto).ToList();
        }

        public async Task<List<TourResponseDto>> SearchToursAdvancedAsync(string? searchTerm, string? category, string? difficulty)
        {
            var query = _context.Tours.Where(t => t.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => t.Title.Contains(searchTerm) ||
                                       t.Description.Contains(searchTerm) ||
                                       t.Location.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.Category == category);
            }

            if (!string.IsNullOrEmpty(difficulty))
            {
                query = query.Where(t => t.DifficultyLevel == difficulty);
            }

            var tours = await query
                .Include(t => t.Images!)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return tours.Select(MapToDto).ToList();
        }

        private TourResponseDto MapToDto(Tour tour)
        {
            var images = tour.Images?
                .Where(img => img != null)
                .OrderBy(img => img.OrderIndex)
                .Select(img => new TourImageDto
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    AltText = img.AltText,
                    IsPrimary = img.IsPrimary,
                    OrderIndex = img.OrderIndex,
                    CreatedAt = img.CreatedAt
                })
                .ToList() ?? new List<TourImageDto>();

            return new TourResponseDto
            {
                Id = tour.Id,
                Title = tour.Title,
                Description = tour.Description,
                ShortDescription = tour.ShortDescription,
                Location = tour.Location,
                DurationHours = tour.DurationHours,
                BasePrice = tour.BasePrice,
                MaxParticipants = tour.MaxParticipants,
                DifficultyLevel = tour.DifficultyLevel,
                Category = tour.Category,
                IsActive = tour.IsActive,
                CreatedAt = tour.CreatedAt,
                UpdatedAt = tour.UpdatedAt,
                Images = images
            };
        }
    }
}