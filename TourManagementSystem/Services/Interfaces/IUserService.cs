using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDto> RegisterAsync(UserRegisterDto registerDto);
        Task<LoginResponseDto> LoginAsync(UserLoginDto loginDto);
        Task<List<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto> GetUserByIdAsync(int id);
        Task<bool> ChangeUserStatusAsync(int id, bool isActive);

        // ✅ ДОБАВЛЕНО: Метод для обновления пользователя
        Task<UserResponseDto> UpdateUserAsync(int userId, UserUpdateDto updateDto);
    }
}