using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwtService;

        public UserService(ApplicationDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<UserResponseDto> RegisterAsync(UserRegisterDto registerDto)
        {
            // 1. Проверяем, нет ли пользователя с таким email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

            if (existingUser != null)
                throw new Exception("Пользователь с таким email уже существует");

            // 2. Валидация роли
            var validRoles = new[] { "Tourist", "Admin" };
            var role = registerDto.Role ?? "Tourist";

            if (!validRoles.Contains(role))
                throw new Exception("Недопустимая роль пользователя");

            // 3. Создаем нового пользователя
            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Phone = registerDto.Phone,
                Role = role,
                IsActive = true
            };

            // 4. Сохраняем в базу данных
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5. Возвращаем DTO (без PasswordHash)
            return new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };
        }

        public async Task<LoginResponseDto> LoginAsync(UserLoginDto loginDto)
        {
            // 1. Ищем активного пользователя по email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

            if (user == null)
                throw new Exception("Пользователь не найден или заблокирован");

            // 2. Проверяем пароль
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                throw new Exception("Неверный пароль");

            // 3. Генерируем JWT токен
            var token = _jwtService.GenerateToken(user);

            // 4. Возвращаем ответ с токеном
            return new LoginResponseDto
            {
                Token = token,
                Expires = DateTime.UtcNow.AddMinutes(60),
                User = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.Phone,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                    IsActive = user.IsActive
                }
            };
        }

        public async Task<List<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.Id)
                .ToListAsync();

            return users.Select(user => new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            }).ToList();
        }

        public async Task<UserResponseDto> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            if (user == null)
                throw new Exception("Пользователь не найден");

            return new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };
        }

        public async Task<bool> ChangeUserStatusAsync(int id, bool isActive)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return false;

            user.IsActive = isActive;
            await _context.SaveChangesAsync();

            return true;
        }

        // ✅ ДОБАВЛЕНО: Метод для обновления пользователя
        public async Task<UserResponseDto> UpdateUserAsync(int userId, UserUpdateDto updateDto)
        {
            // 1. Находим пользователя
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
                throw new Exception("Пользователь не найден");

            // 2. Обновляем основные поля
            if (!string.IsNullOrEmpty(updateDto.FirstName))
                user.FirstName = updateDto.FirstName;

            if (!string.IsNullOrEmpty(updateDto.LastName))
                user.LastName = updateDto.LastName;

            if (!string.IsNullOrEmpty(updateDto.Phone))
                user.Phone = updateDto.Phone;

            // 3. Обрабатываем смену пароля (если указан новый пароль)
            if (!string.IsNullOrEmpty(updateDto.NewPassword))
            {
                // Проверяем текущий пароль (если требуется)
                if (!string.IsNullOrEmpty(updateDto.CurrentPassword))
                {
                    if (!BCrypt.Net.BCrypt.Verify(updateDto.CurrentPassword, user.PasswordHash))
                        throw new Exception("Текущий пароль неверен");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateDto.NewPassword);
            }

            // 4. Сохраняем изменения
            await _context.SaveChangesAsync();

            // 5. Возвращаем обновленные данные
            return new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };
        }
    }
}