using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TourSchedulesController : ControllerBase
    {
        private readonly ITourScheduleService _tourScheduleService;
        private readonly ILogger<TourSchedulesController> _logger;

        public TourSchedulesController(ITourScheduleService tourScheduleService, ILogger<TourSchedulesController> logger)
        {
            _tourScheduleService = tourScheduleService;
            _logger = logger;
        }

        // GET: api/tourschedules
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourScheduleResponseDto>>> GetAllSchedules()
        {
            try
            {
                _logger.LogInformation("Getting all tour schedules");
                var schedules = await _tourScheduleService.GetAllSchedulesAsync();
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tour schedules");
                return StatusCode(500, new { message = "Ошибка при получении расписаний", error = ex.Message });
            }
        }

        // GET: api/tourschedules/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<TourScheduleResponseDto>> GetSchedule(int id)
        {
            try
            {
                _logger.LogInformation($"Getting tour schedule with ID: {id}");
                var schedule = await _tourScheduleService.GetScheduleByIdAsync(id);
                return Ok(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting tour schedule with ID: {id}");
                return NotFound(new { message = ex.Message });
            }
        }

        // GET: api/tourschedules/tour/5
        [HttpGet("tour/{tourId}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourScheduleResponseDto>>> GetSchedulesByTour(int tourId)
        {
            try
            {
                _logger.LogInformation($"Getting schedules for tour ID: {tourId}");
                var schedules = await _tourScheduleService.GetSchedulesByTourAsync(tourId);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting schedules for tour ID: {tourId}");
                return StatusCode(500, new { message = "Ошибка при получении расписаний тура", error = ex.Message });
            }
        }

        // GET: api/tourschedules/available
        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourScheduleResponseDto>>> GetAvailableSchedules()
        {
            try
            {
                _logger.LogInformation("Getting available tour schedules");
                var schedules = await _tourScheduleService.GetAvailableSchedulesAsync();
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available tour schedules");
                return StatusCode(500, new { message = "Ошибка при получении доступных расписаний", error = ex.Message });
            }
        }

        // GET: api/tourschedules/active
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourScheduleResponseDto>>> GetActiveSchedules()
        {
            try
            {
                _logger.LogInformation("Getting active tour schedules");
                var schedules = await _tourScheduleService.GetAvailableSchedulesAsync();

                var activeSchedules = schedules
                    .Where(s => s.StartTime > DateTime.UtcNow &&
                               s.AvailableSlots > 0 &&
                               s.Status == "Scheduled")
                    .OrderBy(s => s.StartTime)
                    .ToList();

                _logger.LogInformation($"Found {activeSchedules.Count} active schedules");
                return Ok(activeSchedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active tour schedules");
                return StatusCode(500, new { message = "Ошибка при получении активных расписаний", error = ex.Message });
            }
        }

        // GET: api/tourschedules/dropdown
        [HttpGet("dropdown")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourScheduleSimpleDto>>> GetSchedulesForDropdown()
        {
            try
            {
                _logger.LogInformation("Getting tour schedules for dropdown");
                var schedules = await _tourScheduleService.GetSchedulesForDropdownAsync();
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tour schedules for dropdown");
                return StatusCode(500, new { message = "Ошибка при получении расписаний для выпадающего списка", error = ex.Message });
            }
        }

        // ✅ ДОБАВЛЕНО: Простой тестовый endpoint для проверки
        [HttpGet("test")]
        [AllowAnonymous]
        public ActionResult Test()
        {
            return Ok(new
            {
                message = "TourSchedulesController работает!",
                timestamp = DateTime.UtcNow,
                endpoints = new[] {
                    "GET /api/tourschedules",
                    "GET /api/tourschedules/tour/{id}",
                    "GET /api/tourschedules/dropdown",
                    "GET /api/tourschedules/available"
                }
            });
        }


        // POST: api/tourschedules - Создание нового расписания
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TourScheduleResponseDto>> CreateSchedule(TourScheduleCreateDto createDto)
        {
            try
            {
                var schedule = await _tourScheduleService.CreateScheduleAsync(createDto);
                return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, schedule);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/tourschedules/{id} - Обновление расписания
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TourScheduleResponseDto>> UpdateSchedule(int id, TourScheduleUpdateDto updateDto)
        {
            try
            {
                var schedule = await _tourScheduleService.UpdateScheduleAsync(id, updateDto);
                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/tourschedules/{id} - Удаление расписания
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteSchedule(int id)
        {
            try
            {
                var result = await _tourScheduleService.DeleteScheduleAsync(id);

                if (!result)
                    return NotFound(new { message = "Расписание не найдено" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
    }
}
