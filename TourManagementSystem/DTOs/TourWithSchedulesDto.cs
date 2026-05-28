namespace TourManagementSystem.DTOs
{
    public class TourWithSchedulesDto
    {
        public TourResponseDto Tour { get; set; } = new TourResponseDto();
        public List<TourScheduleResponseDto> Schedules { get; set; } = new List<TourScheduleResponseDto>();
    }
}