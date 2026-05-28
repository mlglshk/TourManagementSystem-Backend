using Microsoft.AspNetCore.Mvc;
using TourManagementSystem.DTOs;
using TourManagementSystem.Services.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserResponseDto>> Register(UserRegisterDto registerDto)
        {
            try
            {
                var user = await _userService.RegisterAsync(registerDto);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(UserLoginDto loginDto)
        {
            try
            {
                var result = await _userService.LoginAsync(loginDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<UserResponseDto>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserResponseDto>> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // ✅ ДОБАВЛЕНО: Endpoint для обновления пользователя
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, UserUpdateDto updateDto)
        {
            try
            {
                // Проверка: пользователь может обновлять только свой профиль, 
                // если он не администратор
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (currentUserId != id && currentUserRole != "Admin")
                    return Forbid("Вы можете обновлять только свой профиль");

                var updatedUser = await _userService.UpdateUserAsync(id, updateDto);
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH: api/users/{id}/block - Блокировка/разблокировка пользователя
        [HttpPatch("{id}/block")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> BlockUser(int id, [FromBody] bool block)
        {
            try
            {
                // Нельзя заблокировать самого себя
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (currentUserId == id)
                    return BadRequest(new { message = "Вы не можете заблокировать самого себя" });

                var result = await _userService.ChangeUserStatusAsync(id, !block);

                if (!result)
                    return NotFound(new { message = "Пользователь не найден" });

                var status = block ? "заблокирован" : "разблокирован";
                return Ok(new { message = $"Пользователь успешно {status}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}