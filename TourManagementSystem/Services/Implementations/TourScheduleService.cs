using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class TourScheduleService : ITourScheduleService
    {
        private readonly ApplicationDbContext _context;

        public TourScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TourScheduleResponseDto>> GetAllSchedulesAsync()
        {
            var schedules = await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Include(ts => ts.Bookings)
                .Where(ts => ts.Status == "Scheduled")
                .OrderBy(ts => ts.StartTime)
                .ToListAsync();

            return schedules.Select(MapToDto).ToList();
        }

        public async Task<TourScheduleResponseDto> GetScheduleByIdAsync(int id)
        {
            var schedule = await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Include(ts => ts.Bookings)
                .FirstOrDefaultAsync(ts => ts.Id == id && ts.Status == "Scheduled");

            if (schedule == null)
                throw new Exception("Расписание не найдено");

            return MapToDto(schedule);
        }

        public async Task<List<TourScheduleResponseDto>> GetSchedulesByTourAsync(int tourId)
        {
            var schedules = await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Include(ts => ts.Bookings)
                .Where(ts => ts.TourId == tourId && ts.Status == "Scheduled")
                .OrderBy(ts => ts.StartTime)
                .ToListAsync();

            return schedules.Select(MapToDto).ToList();
        }

        public async Task<List<TourScheduleResponseDto>> GetAvailableSchedulesAsync()
        {
            var schedules = await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Include(ts => ts.Bookings)
                .Where(ts => ts.Status == "Scheduled" &&
                            ts.StartTime > DateTime.UtcNow &&
                            ts.AvailableSlots > 0)
                .OrderBy(ts => ts.StartTime)
                .ToListAsync();

            return schedules.Select(MapToDto).ToList();
        }

        public async Task<List<TourScheduleSimpleDto>> GetSchedulesForDropdownAsync()
        {
            var schedules = await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Where(ts => ts.Status == "Scheduled" &&
                            ts.StartTime > DateTime.UtcNow &&
                            ts.AvailableSlots > 0)
                .OrderBy(ts => ts.StartTime)
                .Select(ts => new TourScheduleSimpleDto
                {
                    Id = ts.Id,
                    TourId = ts.TourId,
                    TourTitle = ts.Tour.Title,
                    StartTime = ts.StartTime,
                    EndTime = ts.EndTime,
                    AvailableSlots = ts.AvailableSlots,
                    Price = ts.Price,
                })
                .ToListAsync();

            return schedules;
        }

        private TourScheduleResponseDto MapToDto(TourSchedule schedule)
        {
            var totalBookings = schedule.Bookings?
                .Count(b => b.Status != "Cancelled") ?? 0;

            var totalParticipants = schedule.Bookings?
                .Where(b => b.Status != "Cancelled")
                .Sum(b => b.Participants) ?? 0;

            return new TourScheduleResponseDto
            {
                Id = schedule.Id,
                TourId = schedule.TourId,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                AvailableSlots = schedule.AvailableSlots,
                Status = schedule.Status,
                Price = schedule.Price,
                Notes = schedule.Notes,
                CreatedAt = schedule.CreatedAt,
                UpdatedAt = schedule.UpdatedAt,
                TourTitle = schedule.Tour?.Title,
                TotalBookings = totalBookings,
                TotalParticipants = totalParticipants
            };
        }

        public async Task<TourScheduleResponseDto> CreateScheduleAsync(TourScheduleCreateDto createDto)
        {
            // Проверяем, существует ли тур
            var tour = await _context.Tours.FindAsync(createDto.TourId);
            if (tour == null)
                throw new Exception("Тур не найден");

            // Проверяем корректность дат
            if (createDto.StartTime >= createDto.EndTime)
                throw new Exception("Время начала должно быть раньше времени окончания");

            if (createDto.StartTime < DateTime.UtcNow)
                throw new Exception("Нельзя создать расписание в прошлом");

            // Создаем расписание
            var schedule = new TourSchedule
            {
                TourId = createDto.TourId,
                StartTime = createDto.StartTime,
                EndTime = createDto.EndTime,
                AvailableSlots = createDto.AvailableSlots,
                Price = createDto.Price,
                Notes = createDto.Notes,
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TourSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return await GetScheduleByIdAsync(schedule.Id);
        }

        public async Task<TourScheduleResponseDto> UpdateScheduleAsync(int id, TourScheduleUpdateDto updateDto)
        {
            var schedule = await _context.TourSchedules
                .Include(ts => ts.Tour)
                .Include(ts => ts.Bookings)
                .FirstOrDefaultAsync(ts => ts.Id == id);

            if (schedule == null)
                throw new Exception("Расписание не найдено");

            // Проверяем, есть ли подтвержденные бронирования
            var hasConfirmedBookings = schedule.Bookings != null &&
                schedule.Bookings.Any(b => b.Status == "Confirmed" || b.Status == "Pending");

            // Обновляем поля, если они переданы
            if (updateDto.StartTime.HasValue)
            {
                if (updateDto.StartTime.Value < DateTime.UtcNow)
                    throw new Exception("Нельзя изменить время начала на прошедшую дату");
                schedule.StartTime = updateDto.StartTime.Value;
            }

            if (updateDto.EndTime.HasValue)
            {
                if (updateDto.EndTime.Value <= schedule.StartTime)
                    throw new Exception("Время окончания должно быть позже времени начала");
                schedule.EndTime = updateDto.EndTime.Value;
            }

            if (updateDto.AvailableSlots.HasValue)
            {
                // Нельзя уменьшить количество мест меньше, чем уже забронировано
                var bookedSlots = schedule.Bookings?
                    .Where(b => b.Status != "Cancelled")
                    .Sum(b => b.Participants) ?? 0;

                if (updateDto.AvailableSlots.Value < bookedSlots)
                    throw new Exception($"Нельзя уменьшить количество мест ниже {bookedSlots} (уже забронировано)");

                schedule.AvailableSlots = updateDto.AvailableSlots.Value;
            }

            if (updateDto.Price.HasValue)
            {
                schedule.Price = updateDto.Price.Value;
            }

            if (updateDto.Status != null)
            {
                // Если расписание отменяется, проверяем бронирования
                if (updateDto.Status == "Cancelled" && schedule.Status != "Cancelled" && hasConfirmedBookings)
                {
                    throw new Exception("Нельзя отменить расписание с активными бронированиями");
                }
                schedule.Status = updateDto.Status;
            }

            if (updateDto.Notes != null)
            {
                schedule.Notes = updateDto.Notes;
            }

            schedule.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetScheduleByIdAsync(schedule.Id);
        }

        public async Task<bool> DeleteScheduleAsync(int id)
        {
            var schedule = await _context.TourSchedules
                .Include(ts => ts.Bookings)
                .FirstOrDefaultAsync(ts => ts.Id == id);

            if (schedule == null)
                return false;

            // Проверяем, есть ли бронирования
            var hasAnyBookings = schedule.Bookings != null && schedule.Bookings.Any();
            if (hasAnyBookings)
                throw new Exception("Нельзя удалить расписание, на которое есть бронирования. Сначала отмените бронирования.");

            _context.TourSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}