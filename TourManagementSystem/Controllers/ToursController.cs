using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToursController : ControllerBase
    {
        private readonly ITourService _tourService;
        private readonly ITourScheduleService _tourScheduleService;

        public ToursController(ITourService tourService, ITourScheduleService tourScheduleService)
        {
            _tourService = tourService;
            _tourScheduleService = tourScheduleService;
        }

        // ✅ ДОБАВЛЕНО: Получить тур вместе с расписаниями
        [HttpGet("{id}/with-schedules")]
        [AllowAnonymous]
        public async Task<ActionResult<TourWithSchedulesDto>> GetTourWithSchedules(int id)
        {
            try
            {
                var tour = await _tourService.GetTourByIdAsync(id);
                var schedules = await _tourScheduleService.GetSchedulesByTourAsync(id);

                return Ok(new TourWithSchedulesDto
                {
                    Tour = tour,
                    Schedules = schedules
                });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // ✅ ДОБАВЛЕНО: Получить расписания конкретного тура
        [HttpGet("{id}/schedules")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourScheduleResponseDto>>> GetTourSchedules(int id)
        {
            try
            {
                var schedules = await _tourScheduleService.GetSchedulesByTourAsync(id);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при получении расписаний тура", error = ex.Message });
            }
        }

        // ✅ ДОБАВЛЕНО: Создать тестовые расписания для тура
        [HttpPost("{id}/schedules/create-test")]
        [AllowAnonymous]
        public async Task<ActionResult> CreateTestSchedules(int id)
        {
            try
            {
                // Проверяем существование тура
                var tour = await _tourService.GetTourByIdAsync(id);

                // Создаем тестовые расписания
                var testSchedules = new List<TourScheduleCreateDto>
                {
                    new TourScheduleCreateDto
                    {
                        TourId = id,
                        StartTime = DateTime.UtcNow.AddDays(1),
                        EndTime = DateTime.UtcNow.AddDays(1).AddHours(6),
                        AvailableSlots = 8,
                        Price = tour.BasePrice,
                        Notes = "Тестовое расписание 1"
                    },
                    new TourScheduleCreateDto
                    {
                        TourId = id,
                        StartTime = DateTime.UtcNow.AddDays(3),
                        EndTime = DateTime.UtcNow.AddDays(3).AddHours(6),
                        AvailableSlots = 5,
                        Price = tour.BasePrice,
                        Notes = "Тестовое расписание 2"
                    }
                };

                // Здесь нужно добавить логику создания расписаний
                // Пока просто возвращаем информацию
                return Ok(new
                {
                    message = $"Создано бы {testSchedules.Count} тестовых расписаний для тура '{tour.Title}'",
                    tourId = id,
                    schedules = testSchedules.Select(s => new {
                        startTime = s.StartTime,
                        availableSlots = s.AvailableSlots,
                        price = s.Price
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при создании тестовых расписаний", error = ex.Message });
            }
        }

        // GET: api/tours
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourResponseDto>>> GetAllTours()
        {
            var tours = await _tourService.GetAllToursAsync();
            return Ok(tours);
        }

        // GET: api/tours/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<TourResponseDto>> GetTour(int id)
        {
            try
            {
                var tour = await _tourService.GetTourByIdAsync(id);
                return Ok(tour);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST: api/tours
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TourResponseDto>> CreateTour(TourCreateDto createDto)
        {
            try
            {
                var tour = await _tourService.CreateTourAsync(createDto);
                return CreatedAtAction(nameof(GetTour), new { id = tour.Id }, tour);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/tours/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TourResponseDto>> UpdateTour(int id, TourUpdateDto updateDto)
        {
            try
            {
                var tour = await _tourService.UpdateTourAsync(id, updateDto);
                return Ok(tour);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/tours/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTour(int id)
        {
            var result = await _tourService.DeleteTourAsync(id);

            if (!result)
                return NotFound(new { message = "Тур не найден" });

            return NoContent();
        }

        // PATCH: api/tours/5/status
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ToggleTourStatus(int id, [FromBody] bool isActive)
        {
            var result = await _tourService.ToggleTourStatusAsync(id, isActive);

            if (!result)
                return NotFound(new { message = "Тур не найден" });

            return Ok(new { message = $"Тур {(isActive ? "активирован" : "деактивирован")}" });
        }

        // GET: api/tours/search?q=поисковый_запрос
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourResponseDto>>> SearchTours([FromQuery] string q)
        {
            var tours = await _tourService.SearchToursAsync(q);
            return Ok(tours);
        }

        // GET: api/tours/category/приключения
        [HttpGet("category/{category}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourResponseDto>>> GetToursByCategory(string category)
        {
            var tours = await _tourService.GetToursByCategoryAsync(category);
            return Ok(tours);
        }

        // GET: api/tours/difficulty/легкий
        [HttpGet("difficulty/{difficulty}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourResponseDto>>> GetToursByDifficulty(string difficulty)
        {
            var tours = await _tourService.GetToursByDifficultyAsync(difficulty);
            return Ok(tours);
        }

        // GET: api/tours/advanced-search?search=...&category=...&difficulty=...
        [HttpGet("advanced-search")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourResponseDto>>> AdvancedSearch(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] string? difficulty)
        {
            var tours = await _tourService.SearchToursAdvancedAsync(search, category, difficulty);
            return Ok(tours);
        }
    }
}