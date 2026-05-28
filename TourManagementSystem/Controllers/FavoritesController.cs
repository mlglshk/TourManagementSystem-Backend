using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Только авторизованные
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        // GET: api/favorites - мои избранные туры
        [HttpGet]
        public async Task<ActionResult<List<TourResponseDto>>> GetMyFavorites()
        {
            var userId = GetCurrentUserId();
            var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
            return Ok(favorites);
        }

        // POST: api/favorites/{tourId} - добавить в избранное
        [HttpPost("{tourId}")]
        public async Task<ActionResult> AddToFavorites(int tourId)
        {
            var userId = GetCurrentUserId();
            var result = await _favoriteService.AddToFavoritesAsync(userId, tourId);

            if (!result)
                return BadRequest(new { message = "Тур уже в избранном или не найден" });

            return Ok(new { message = "Тур добавлен в избранное" });
        }

        // DELETE: api/favorites/{tourId} - удалить из избранного
        [HttpDelete("{tourId}")]
        public async Task<ActionResult> RemoveFromFavorites(int tourId)
        {
            var userId = GetCurrentUserId();
            var result = await _favoriteService.RemoveFromFavoritesAsync(userId, tourId);

            if (!result)
                return NotFound(new { message = "Тур не найден в избранном" });

            return Ok(new { message = "Тур удален из избранного" });
        }

        // GET: api/favorites/{tourId}/check - проверить, в избранном ли
        [HttpGet("{tourId}/check")]
        public async Task<ActionResult> CheckIsFavorite(int tourId)
        {
            var userId = GetCurrentUserId();
            var isFavorite = await _favoriteService.IsTourInFavoritesAsync(userId, tourId);

            return Ok(new { isFavorite });
        }

        // Вспомогательный метод
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}