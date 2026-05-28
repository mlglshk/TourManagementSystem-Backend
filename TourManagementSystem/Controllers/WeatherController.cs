using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        // GET: api/weather/forecast?city=Moscow&date=2026-04-10
        [HttpGet("forecast")]
        [AllowAnonymous]
        public async Task<ActionResult<WeatherResponseDto>> GetWeatherForecast(
            [FromQuery] string city,
            [FromQuery] DateTime date)
        {
            try
            {
                var weather = await _weatherService.GetWeatherForecastAsync(city, date);
                return Ok(weather);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/weather/current?city=Moscow
        [HttpGet("current")]
        [AllowAnonymous]
        public async Task<ActionResult<CurrentWeatherResponseDto>> GetCurrentWeather([FromQuery] string city)
        {
            try
            {
                var weather = await _weatherService.GetCurrentWeatherAsync(city);
                return Ok(weather);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/weather/coordinates?lat=55.75&lon=37.62&date=2026-04-10
        [HttpGet("coordinates")]
        [AllowAnonymous]
        public async Task<ActionResult<WeatherResponseDto>> GetWeatherByCoordinates(
            [FromQuery] double lat,
            [FromQuery] double lon,
            [FromQuery] DateTime date)
        {
            try
            {
                var weather = await _weatherService.GetWeatherForecastByCoordinatesAsync(lat, lon, date);
                return Ok(weather);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/weather/tour-schedule/5
        [HttpGet("tour-schedule/{scheduleId}")]
        [AllowAnonymous]
        public async Task<ActionResult<WeatherResponseDto>> GetWeatherForTourSchedule(int scheduleId)
        {
            try
            {
                var weather = await _weatherService.GetWeatherForTourScheduleAsync(scheduleId);
                return Ok(weather);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/weather/test
        [HttpGet("test")]
        [AllowAnonymous]
        public ActionResult Test()
        {
            return Ok(new
            {
                message = "Weather API is working!",
                note = "Add your OpenWeatherMap API key to appsettings.json",
                howToGetKey = "https://home.openweathermap.org/api_keys"
            });
        }

        // POST: api/weather/batch
        [HttpPost("batch")]
        [AllowAnonymous]
        public async Task<ActionResult<List<WeatherResponseDto>>> GetBatchWeather(
            [FromBody] BatchWeatherRequestDto request)
        {
            var results = new List<WeatherResponseDto>();
            foreach (var item in request.Items)
            {
                try
                {
                    var weather = await _weatherService.GetWeatherForecastAsync(item.City, item.Date);
                    results.Add(weather);
                }
                catch
                {
                    results.Add(null);
                }
            }
            return Ok(results);
        }
    }
}