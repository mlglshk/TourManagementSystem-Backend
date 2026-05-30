using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class WeatherService : IWeatherService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeatherService> _logger;
        private readonly string _apiKey;
        private const string BASE_URL = "https://api.openweathermap.org/data/2.5";

        public WeatherService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<WeatherService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
            _apiKey = configuration["WeatherApi:Key"] ?? throw new Exception("Weather API Key not configured");
        }

        public async Task<WeatherResponseDto> GetWeatherForecastAsync(string city, DateTime date)
        {
            // Нормализуем дату (без времени)
            var targetDate = DateOnly.FromDateTime(date.Date);
            var cacheKey = $"weather_{city}_{targetDate}";

            // 1. Проверяем кэш памяти
            if (_cache.TryGetValue(cacheKey, out WeatherResponseDto? cachedResult) && cachedResult != null)
            {
                cachedResult.IsCached = true;
                return cachedResult;
            }

            // 2. Проверяем базу данных
            var dbCache = await _context.Set<WeatherCache>()
                .FirstOrDefaultAsync(w => w.Location == city && w.ForecastDate == targetDate && w.ExpiresAt > DateTime.UtcNow);

            if (dbCache != null)
            {
                var result = MapFromCache(dbCache, city);
                // Сохраняем в memory cache на 1 час
                _cache.Set(cacheKey, result, TimeSpan.FromHours(1));
                result.IsCached = true;
                return result;
            }

            // 3. Запрашиваем из внешнего API
            try
            {
                var weatherData = await FetchWeatherFromApi(city, targetDate);

                // Сохраняем в БД
                await SaveToCache(city, targetDate, weatherData);

                // Сохраняем в memory cache
                _cache.Set(cacheKey, weatherData, TimeSpan.FromHours(1));
                weatherData.IsCached = false;

                return weatherData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка получения погоды для {city}");
                throw new Exception("Не удалось получить данные о погоде");
            }
        }

        public async Task<WeatherResponseDto> GetWeatherForecastByCoordinatesAsync(double lat, double lon, DateTime date)
        {
            // Получаем название города по координатам
            var city = await GetCityNameByCoordinates(lat, lon);
            return await GetWeatherForecastAsync(city, date);
        }

        public async Task<CurrentWeatherResponseDto> GetCurrentWeatherAsync(string city)
        {
            // Маппинг русских названий на английские
            var map = new Dictionary<string, string>
            {
                {"Москва", "Moscow"},
                {"Первоуральск", "Pervouralsk"},
                {"Екатеринбург", "Yekaterinburg"},
                {"Санкт-Петербург", "Saint Petersburg"},
                {"Краснодар", "Krasnodar"}
            };

            if (map.ContainsKey(city))
                city = map[city];

            var cacheKey = $"current_weather_{city}";

            if (_cache.TryGetValue(cacheKey, out CurrentWeatherResponseDto? cachedResult) && cachedResult != null)
            {
                return cachedResult;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{BASE_URL}/weather?q={city}&appid={_apiKey}&units=metric&lang=ru";

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                var result = new CurrentWeatherResponseDto
                {
                    Location = city,
                    ObservedAt = DateTime.UtcNow,
                    Temperature = Math.Round(data.GetProperty("main").GetProperty("temp").GetDecimal(), 1),
                    Condition = GetRussianCondition(data.GetProperty("weather")[0].GetProperty("description").GetString() ?? ""),
                    Humidity = data.GetProperty("main").GetProperty("humidity").GetInt32(),
                    WindSpeed = Math.Round(data.GetProperty("wind").GetProperty("speed").GetDecimal(), 1),
                    Icon = $"https://openweathermap.org/img/w/{data.GetProperty("weather")[0].GetProperty("icon").GetString()}.png"
                };

                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка получения текущей погоды для {city}");
                throw new Exception("Не удалось получить текущую погоду");
            }
        }

        public async Task<WeatherResponseDto> GetWeatherForTourScheduleAsync(int scheduleId)
        {
            var schedule = await _context.TourSchedules
                .Include(ts => ts.Tour)
                .FirstOrDefaultAsync(ts => ts.Id == scheduleId);

            if (schedule == null)
                throw new Exception("Расписание тура не найдено");

            var location = schedule.Tour?.Location ?? "Moscow";
            return await GetWeatherForecastAsync(location, schedule.StartTime);
        }

        private async Task<WeatherResponseDto> FetchWeatherFromApi(string city, DateOnly targetDate)
        {
            var client = _httpClientFactory.CreateClient();

            // Сначала пробуем получить прогноз (5 дней)
            var forecastUrl = $"{BASE_URL}/forecast?q={city}&appid={_apiKey}&units=metric&lang=ru";

            try
            {
                var response = await client.GetAsync(forecastUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(json);

                    // Ищем прогноз на нужную дату
                    var forecasts = data.GetProperty("list").EnumerateArray();
                    var targetForecast = forecasts
                        .FirstOrDefault(f => DateTime.Parse(f.GetProperty("dt_txt").GetString()!).Date == targetDate.ToDateTime(TimeOnly.MinValue).Date);

                    if (targetForecast.ValueKind != JsonValueKind.Undefined)
                    {
                        return ParseForecast(targetForecast, city, targetDate);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось получить прогноз погоды, используем текущую погоду");
            }

            // Если прогноза нет (дата дальше 5 дней или ошибка) — используем текущую погоду
            return await GetCurrentWeatherAsForecast(city, targetDate);
        }

        private async Task<WeatherResponseDto> GetCurrentWeatherAsForecast(string city, DateOnly targetDate)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{BASE_URL}/weather?q={city}&appid={_apiKey}&units=metric&lang=ru";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            var temp = data.GetProperty("main").GetProperty("temp").GetDecimal();
            var condition = GetRussianCondition(data.GetProperty("weather")[0].GetProperty("description").GetString() ?? "");
            var humidity = data.GetProperty("main").GetProperty("humidity").GetInt32();
            var windSpeed = data.GetProperty("wind").GetProperty("speed").GetDecimal();
            var icon = data.GetProperty("weather")[0].GetProperty("icon").GetString();

            return new WeatherResponseDto
            {
                Location = city,
                ForecastDate = targetDate.ToDateTime(TimeOnly.MinValue),
                TemperatureMin = Math.Round(temp - 2, 1),  // Примерный минимум
                TemperatureMax = Math.Round(temp + 2, 1),  // Примерный максимум
                Condition = condition,
                Humidity = humidity,
                WindSpeed = Math.Round(windSpeed, 1),
                Icon = $"https://openweathermap.org/img/w/{icon}.png",
                Recommendation = GetWeatherRecommendation(temp - 2, temp + 2, condition),
                IsCached = false
            };
        }

        private WeatherResponseDto ParseForecast(JsonElement forecast, string city, DateOnly targetDate)
        {
            var tempMin = forecast.GetProperty("main").GetProperty("temp_min").GetDecimal();
            var tempMax = forecast.GetProperty("main").GetProperty("temp_max").GetDecimal();
            var condition = GetRussianCondition(forecast.GetProperty("weather")[0].GetProperty("description").GetString() ?? "");
            var humidity = forecast.GetProperty("main").GetProperty("humidity").GetInt32();
            var windSpeed = forecast.GetProperty("wind").GetProperty("speed").GetDecimal();
            var icon = forecast.GetProperty("weather")[0].GetProperty("icon").GetString();

            return new WeatherResponseDto
            {
                Location = city,
                ForecastDate = targetDate.ToDateTime(TimeOnly.MinValue),
                TemperatureMin = Math.Round(tempMin, 1),
                TemperatureMax = Math.Round(tempMax, 1),
                Condition = condition,
                Humidity = humidity,
                WindSpeed = Math.Round(windSpeed, 1),
                Icon = $"https://openweathermap.org/img/w/{icon}.png",
                Recommendation = GetWeatherRecommendation(tempMin, tempMax, condition),
                IsCached = false
            };
        }

        private async Task<string> GetCityNameByCoordinates(double lat, double lon)
        {
            var cacheKey = $"geocode_{lat}_{lon}";

            if (_cache.TryGetValue(cacheKey, out string? city) && city != null)
            {
                return city;
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"http://api.openweathermap.org/geo/1.0/reverse?lat={lat}&lon={lon}&limit=1&appid={_apiKey}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement[]>(json);

            if (data == null || data.Length == 0)
                throw new Exception("Не удалось определить город по координатам");

            city = data[0].GetProperty("name").GetString() ?? "Moscow";
            _cache.Set(cacheKey, city, TimeSpan.FromDays(7));

            return city;
        }

        private async Task SaveToCache(string city, DateOnly date, WeatherResponseDto weather)
        {
            var cacheEntry = new WeatherCache
            {
                Location = city,
                ForecastDate = date,
                TemperatureMin = weather.TemperatureMin,
                TemperatureMax = weather.TemperatureMax,
                Condition = weather.Condition,
                Humidity = weather.Humidity,
                WindSpeed = weather.WindSpeed,
                Icon = weather.Icon,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(6) // Кэш на 6 часов
            };

            _context.Set<WeatherCache>().Add(cacheEntry);
            await _context.SaveChangesAsync();
        }

        private WeatherResponseDto MapFromCache(WeatherCache cache, string city)
        {
            return new WeatherResponseDto
            {
                Location = city,
                ForecastDate = cache.ForecastDate.ToDateTime(TimeOnly.MinValue),
                TemperatureMin = cache.TemperatureMin,
                TemperatureMax = cache.TemperatureMax,
                Condition = cache.Condition,
                Humidity = cache.Humidity,
                WindSpeed = cache.WindSpeed,
                Icon = cache.Icon,
                Recommendation = GetWeatherRecommendation(cache.TemperatureMin ?? 0, cache.TemperatureMax ?? 0, cache.Condition ?? ""),
                IsCached = true
            };
        }

        private string GetRussianCondition(string englishCondition)
        {
            var translations = new Dictionary<string, string>
            {
                { "clear sky", "ясно" },
                { "few clouds", "малооблачно" },
                { "scattered clouds", "облачно с прояснениями" },
                { "broken clouds", "облачно" },
                { "overcast clouds", "пасмурно" },
                { "light rain", "небольшой дождь" },
                { "moderate rain", "дождь" },
                { "heavy rain", "сильный дождь" },
                { "shower rain", "ливень" },
                { "thunderstorm", "гроза" },
                { "snow", "снег" },
                { "light snow", "небольшой снег" },
                { "mist", "туман" },
                { "fog", "туман" }
            };

            return translations.ContainsKey(englishCondition) ? translations[englishCondition] : englishCondition;
        }

        private string GetWeatherRecommendation(decimal tempMin, decimal tempMax, string condition)
        {
            var avgTemp = (tempMin + tempMax) / 2;

            if (condition.Contains("дождь") || condition.Contains("ливень"))
                return "📌 Возьмите зонт и непромокаемую одежду";

            if (condition.Contains("снег"))
                return "📌 Одевайтесь тепло, возможен снегопад";

            if (avgTemp < 0)
                return "📌 Очень холодно! Теплая одежда обязательна";

            if (avgTemp < 10)
                return "📌 Прохладно, возьмите куртку или ветровку";

            if (avgTemp < 20)
                return "📌 Комфортная погода для экскурсии";

            if (avgTemp < 30)
                return "📌 Тепло, не забудьте головной убор и воду";

            return "📌 Жарко! Рекомендуем легкую одежду и солнцезащитный крем";
        }
    }
}