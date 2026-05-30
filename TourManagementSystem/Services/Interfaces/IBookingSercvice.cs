using TourManagementSystem.DTOs;

namespace TourManagementSystem.Services.Interfaces
{
    public interface IBookingService
    {
        // ОСНОВНЫЕ МЕТОДЫ ДЛЯ БРОНИРОВАНИЯ
        Task<BookingResponseDto> GetBookingByIdAsync(int id);
        Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto createDto);
        Task<bool> CancelBookingAsync(int id, string cancellationReason);
        Task<List<BookingResponseDto>> GetUserBookingsAsync(int userId);

        // ✅ ДОБАВЛЕНО: Методы для получения всех бронирований и обновления
        Task<List<BookingResponseDto>> GetAllBookingsAsync();
        Task<BookingResponseDto> UpdateBookingAsync(int id, BookingUpdateDto updateDto);
        Task<bool> UpdateBookingStatusAsync(int id, string status);

        // ✅ ДОБАВЛЕНО: Методы фильтрации
        Task<List<BookingResponseDto>> GetBookingsByStatusAsync(string status);
        Task<List<BookingResponseDto>> GetBookingsByUserEmailAsync(string email);
        Task<List<BookingResponseDto>> SearchBookingsAsync(string? status, string? email);

        // Метод для создания бронирования администратором (без аккаунта)
        Task<BookingResponseDto> CreateAdminBookingWithoutAccountAsync(AdminBookingCreateDto createDto);
        // Получить только гостевые бронирования (где userId = null)
        Task<List<BookingResponseDto>> GetGuestBookingsAsync();
    }
}