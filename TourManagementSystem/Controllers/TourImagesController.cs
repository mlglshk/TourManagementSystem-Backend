using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Только админы могут управлять фото
    public class TourImagesController : ControllerBase
    {
        private readonly ITourImageService _tourImageService;

        public TourImagesController(ITourImageService tourImageService)
        {
            _tourImageService = tourImageService;
        }

        // GET: api/tourimages/tour/5 - получить все фото тура (доступно всем)
        [HttpGet("tour/{tourId}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<TourImageDto>>> GetImagesByTour(int tourId)
        {
            var images = await _tourImageService.GetImagesByTourAsync(tourId);
            return Ok(images);
        }

        // POST: api/tourimages/tour/5 - добавить фото к туру
        [HttpPost("tour/{tourId}")]
        public async Task<ActionResult<TourImageDto>> AddImage(int tourId, TourImageCreateDto createDto)
        {
            try
            {
                var image = await _tourImageService.AddImageAsync(tourId, createDto);
                return CreatedAtAction(nameof(GetImagesByTour), new { tourId = tourId }, image);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/tourimages/5 - обновить фото
        [HttpPut("{id}")]
        public async Task<ActionResult<TourImageDto>> UpdateImage(int id, TourImageUpdateDto updateDto)
        {
            try
            {
                var image = await _tourImageService.UpdateImageAsync(id, updateDto);
                return Ok(image);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/tourimages/5 - удалить фото
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteImage(int id)
        {
            var result = await _tourImageService.DeleteImageAsync(id);

            if (!result)
                return NotFound(new { message = "Фото не найдено" });

            return NoContent();
        }

        // PATCH: api/tourimages/tour/5/primary/10 - сделать фото главным
        [HttpPatch("tour/{tourId}/primary/{imageId}")]
        public async Task<ActionResult<TourImageDto>> SetPrimaryImage(int tourId, int imageId)
        {
            try
            {
                var image = await _tourImageService.SetPrimaryImageAsync(tourId, imageId);
                return Ok(image);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}