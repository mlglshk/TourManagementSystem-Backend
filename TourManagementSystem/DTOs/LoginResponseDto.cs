// DTOs/LoginResponseDto.cs
namespace TourManagementSystem.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public UserResponseDto User { get; set; } = new UserResponseDto();
    }
}