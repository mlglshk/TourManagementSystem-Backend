using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        // GET: api/reviews/tour/5
        [HttpGet("tour/{tourId}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ReviewResponseDto>>> GetReviewsByTour(int tourId)
        {
            try
            {
                var reviews = await _reviewService.GetReviewsByTourAsync(tourId);
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // GET: api/reviews/tour/5/rating
        [HttpGet("tour/{tourId}/rating")]
        [AllowAnonymous]
        public async Task<ActionResult<TourRatingSummaryDto>> GetTourRating(int tourId)
        {
            try
            {
                var rating = await _reviewService.GetTourRatingSummaryAsync(tourId);
                return Ok(rating);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // GET: api/reviews/user
        [HttpGet("user")]
        [Authorize]
        public async Task<ActionResult<List<ReviewResponseDto>>> GetMyReviews()
        {
            var userId = GetCurrentUserId();
            var reviews = await _reviewService.GetReviewsByUserAsync(userId);
            return Ok(reviews);
        }

        // GET: api/reviews/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ReviewResponseDto>> GetReview(int id)
        {
            try
            {
                var review = await _reviewService.GetReviewByIdAsync(id);
                return Ok(review);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST: api/reviews
        [HttpPost]
        [Authorize(Roles = "Tourist")]
        public async Task<ActionResult<ReviewResponseDto>> CreateReview(ReviewCreateDto createDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var review = await _reviewService.CreateReviewAsync(userId, createDto);
                return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/reviews/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ReviewResponseDto>> UpdateReview(int id, ReviewUpdateDto updateDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");
                var review = await _reviewService.UpdateReviewAsync(id, userId, isAdmin, updateDto);
                return Ok(review);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/reviews/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteReview(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");
                var result = await _reviewService.DeleteReviewAsync(id, userId, isAdmin);

                if (!result)
                    return NotFound(new { message = "Отзыв не найден" });

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH: api/reviews/5/verify
        [HttpPatch("{id}/verify")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> VerifyReview(int id, [FromBody] bool isVerified)
        {
            try
            {
                var result = await _reviewService.VerifyReviewAsync(id, isVerified);

                if (!result)
                    return NotFound(new { message = "Отзыв не найден" });

                return Ok(new { message = $"Отзыв {(isVerified ? "верифицирован" : "деверифицирован")}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/reviews/all (только админ)
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<ReviewResponseDto>>> GetAllReviews()
        {
            var reviews = await _reviewService.GetAllReviewsAsync();
            return Ok(reviews);
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }
    }
}