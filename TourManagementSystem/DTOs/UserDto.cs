using System.ComponentModel.DataAnnotations;

namespace TourManagementSystem.DTOs
{
    public class UserRegisterDto
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required][MinLength(6)] public string Password { get; set; } = string.Empty;
        [StringLength(100)] public string? FirstName { get; set; }
        [StringLength(100)] public string? LastName { get; set; }
        [Phone] public string? Phone { get; set; }
        public string? Role { get; set; }
    }

    public class UserLoginDto
    {
        [Required] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    // ✅ ДОБАВЛЕНО: DTO для обновления пользователя (в том же файле)
    public class UserUpdateDto
    {
        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [MinLength(6)]
        public string? NewPassword { get; set; }

        public string? CurrentPassword { get; set; } // Для проверки текущего пароля при смене
    }
}