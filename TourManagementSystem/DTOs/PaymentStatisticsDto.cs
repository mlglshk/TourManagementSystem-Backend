// TourManagementSystem/DTOs/PaymentStatisticsDto.cs
namespace TourManagementSystem.DTOs
{
    public class PaymentStatisticsDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public int SuccessfulCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }
        public int RefundedCount { get; set; }
        public double SuccessRate { get; set; }
        public decimal AverageCheck { get; set; }

        // По платежным методам
        public Dictionary<string, PaymentMethodStats> PaymentMethods { get; set; } = new();

        // Ежедневная динамика (за последние 7 дней)
        public List<DailyPaymentStats> DailyStats { get; set; } = new();
    }

    public class PaymentMethodStats
    {
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class DailyPaymentStats
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}