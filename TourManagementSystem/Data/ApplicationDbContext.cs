using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Models;

namespace TourManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSet для каждой таблицы
        public DbSet<User> Users { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<TourSchedule> TourSchedules { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<TourImage> TourImages { get; set; }
        public DbSet<PaymentHistory> PaymentHistories { get; set; }
        public DbSet<FavoriteTour> FavoriteTours { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }

        // ✅ НОВЫЕ DbSet
        public DbSet<Review> Reviews { get; set; }
        public DbSet<WeatherCache> WeatherCaches { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== СУЩЕСТВУЮЩИЕ ТАБЛИЦЫ ====================

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Tour>().ToTable("tours");
            modelBuilder.Entity<TourSchedule>().ToTable("tour_schedules");
            modelBuilder.Entity<Booking>().ToTable("bookings");
            modelBuilder.Entity<Payment>().ToTable("payments");
            modelBuilder.Entity<TourImage>().ToTable("tour_images");

            // Конфигурация для User
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Id).HasColumnName("id");
                entity.Property(u => u.Email).HasColumnName("email");
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
                entity.Property(u => u.FirstName).HasColumnName("first_name");
                entity.Property(u => u.LastName).HasColumnName("last_name");
                entity.Property(u => u.Phone).HasColumnName("phone");
                entity.Property(u => u.Role).HasColumnName("role");
                entity.Property(u => u.CreatedAt).HasColumnName("created_at");
                entity.Property(u => u.IsActive).HasColumnName("is_active");

                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Role).HasConversion<string>();
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(u => u.IsActive).HasDefaultValue(true);
            });

            // Конфигурация для Tour
            modelBuilder.Entity<Tour>(entity =>
            {
                entity.Property(t => t.Id).HasColumnName("id");
                entity.Property(t => t.Title).HasColumnName("title");
                entity.Property(t => t.Description).HasColumnName("description");
                entity.Property(t => t.ShortDescription).HasColumnName("short_description");
                entity.Property(t => t.Location).HasColumnName("location");
                entity.Property(t => t.DurationHours).HasColumnName("duration_hours");
                entity.Property(t => t.BasePrice).HasColumnName("base_price").HasColumnType("decimal(10,2)");
                entity.Property(t => t.MaxParticipants).HasColumnName("max_participants");
                entity.Property(t => t.DifficultyLevel).HasColumnName("difficulty_level");
                entity.Property(t => t.Category).HasColumnName("category");
                entity.Property(t => t.IsActive).HasColumnName("is_active");
                entity.Property(t => t.CreatedAt).HasColumnName("created_at");
                entity.Property(t => t.UpdatedAt).HasColumnName("updated_at");

                entity.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(t => t.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(t => t.IsActive).HasDefaultValue(true);
            });

            // Конфигурация для TourSchedule
            modelBuilder.Entity<TourSchedule>(entity =>
            {
                entity.Property(ts => ts.Id).HasColumnName("id");
                entity.Property(ts => ts.TourId).HasColumnName("tour_id");
                entity.Property(ts => ts.StartTime).HasColumnName("start_time");
                entity.Property(ts => ts.EndTime).HasColumnName("end_time");
                entity.Property(ts => ts.AvailableSlots).HasColumnName("available_slots");
                entity.Property(ts => ts.Status).HasColumnName("status");
                entity.Property(ts => ts.Price).HasColumnName("price").HasColumnType("decimal(10,2)");
                entity.Property(ts => ts.Notes).HasColumnName("notes");
                entity.Property(ts => ts.CreatedAt).HasColumnName("created_at");
                entity.Property(ts => ts.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(ts => ts.Tour)
                      .WithMany(t => t.Schedules)
                      .HasForeignKey(ts => ts.TourId)
                      .OnDelete(DeleteBehavior.Cascade);

                

                entity.Property(ts => ts.Status).HasDefaultValue("Scheduled");
                entity.Property(ts => ts.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(ts => ts.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Конфигурация для Booking
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.Property(b => b.Id).HasColumnName("id");
                entity.Property(b => b.BookingNumber).HasColumnName("booking_number");
                entity.Property(b => b.UserId).HasColumnName("user_id");
                entity.Property(b => b.TourScheduleId).HasColumnName("tour_schedule_id");
                entity.Property(b => b.Participants).HasColumnName("participants");
                entity.Property(b => b.TotalPrice).HasColumnName("total_price").HasColumnType("decimal(10,2)");
                entity.Property(b => b.BookingDate).HasColumnName("booking_date");
                entity.Property(b => b.Status).HasColumnName("status");
                entity.Property(b => b.SpecialRequirements).HasColumnName("special_requirements");
                entity.Property(b => b.CancelledAt).HasColumnName("cancelled_at");
                entity.Property(b => b.CancellationReason).HasColumnName("cancellation_reason");

                entity.HasIndex(b => b.BookingNumber).IsUnique();

                entity.HasOne(b => b.User)
                      .WithMany(u => u.Bookings)
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(b => b.TourSchedule)
                      .WithMany(ts => ts.Bookings)
                      .HasForeignKey(b => b.TourScheduleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(b => b.BookingDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(b => b.Status).HasDefaultValue("Pending");
            });

            // Конфигурация для Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Id).HasColumnName("id");
                entity.Property(p => p.BookingId).HasColumnName("booking_id");
                entity.Property(p => p.Amount).HasColumnName("amount").HasColumnType("decimal(10,2)");
                entity.Property(p => p.PaymentDate).HasColumnName("payment_date");
                entity.Property(p => p.PaymentMethod).HasColumnName("payment_method");
                entity.Property(p => p.Status).HasColumnName("status");
                entity.Property(p => p.TransactionId).HasColumnName("transaction_id");
                entity.Property(p => p.Notes).HasColumnName("notes");
                entity.Property(p => p.CreatedAt).HasColumnName("created_at");
                entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(p => p.Booking)
                      .WithMany(b => b.Payments)
                      .HasForeignKey(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(p => p.Status).HasDefaultValue("Pending");
                entity.Property(p => p.PaymentMethod).HasDefaultValue("Card");
                entity.Property(p => p.PaymentDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(p => p.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Конфигурация для TourImage
            modelBuilder.Entity<TourImage>(entity =>
            {
                entity.Property(ti => ti.Id).HasColumnName("id");
                entity.Property(ti => ti.TourId).HasColumnName("tour_id");
                entity.Property(ti => ti.ImageUrl).HasColumnName("image_url");
                entity.Property(ti => ti.AltText).HasColumnName("alt_text");
                entity.Property(ti => ti.IsPrimary).HasColumnName("is_primary");
                entity.Property(ti => ti.OrderIndex).HasColumnName("order_index");
                entity.Property(ti => ti.CreatedAt).HasColumnName("created_at");

                entity.HasOne(ti => ti.Tour)
                      .WithMany(t => t.Images)
                      .HasForeignKey(ti => ti.TourId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(ti => ti.IsPrimary).HasDefaultValue(false);
                entity.Property(ti => ti.OrderIndex).HasDefaultValue(0);
                entity.Property(ti => ti.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Конфигурация для PaymentHistory
            modelBuilder.Entity<PaymentHistory>().ToTable("payment_histories");
            modelBuilder.Entity<PaymentHistory>(entity =>
            {
                entity.Property(ph => ph.Id).HasColumnName("id");
                entity.Property(ph => ph.PaymentId).HasColumnName("payment_id");
                entity.Property(ph => ph.Status).HasColumnName("status");
                entity.Property(ph => ph.Notes).HasColumnName("notes");
                entity.Property(ph => ph.CreatedAt).HasColumnName("created_at");

                entity.HasOne(ph => ph.Payment)
                      .WithMany()
                      .HasForeignKey(ph => ph.PaymentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(ph => ph.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Конфигурация для FavoriteTour
            modelBuilder.Entity<FavoriteTour>().ToTable("favorite_tours");
            modelBuilder.Entity<FavoriteTour>(entity =>
            {
                entity.Property(f => f.Id).HasColumnName("id");
                entity.Property(f => f.UserId).HasColumnName("user_id");
                entity.Property(f => f.TourId).HasColumnName("tour_id");
                entity.Property(f => f.AddedAt).HasColumnName("added_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(f => new { f.UserId, f.TourId }).IsUnique();

                entity.HasOne(f => f.User)
                      .WithMany()
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Tour)
                      .WithMany()
                      .HasForeignKey(f => f.TourId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Конфигурация для EmailTemplate
            modelBuilder.Entity<EmailTemplate>().ToTable("email_templates");
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.Property(et => et.Id).HasColumnName("id");
                entity.Property(et => et.TemplateName).HasColumnName("template_name");
                entity.Property(et => et.Subject).HasColumnName("subject");
                entity.Property(et => et.Body).HasColumnName("body");
                entity.Property(et => et.IsActive).HasColumnName("is_active");
                entity.Property(et => et.CreatedAt).HasColumnName("created_at");
                entity.Property(et => et.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(et => et.TemplateName).IsUnique();

                entity.Property(et => et.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(et => et.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(et => et.IsActive).HasDefaultValue(true);
            });

            // ==================== НОВЫЕ ТАБЛИЦЫ ====================

            // ✅ Конфигурация для Review
            modelBuilder.Entity<Review>(entity =>
            {
                entity.ToTable("reviews");

                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.TourId).HasColumnName("tour_id");
                entity.Property(r => r.UserId).HasColumnName("user_id");
                entity.Property(r => r.Rating).HasColumnName("rating");
                entity.Property(r => r.Comment).HasColumnName("comment");
                entity.Property(r => r.CreatedAt).HasColumnName("created_at");
                entity.Property(r => r.UpdatedAt).HasColumnName("updated_at");
                entity.Property(r => r.IsVerified).HasColumnName("is_verified");

                // Внешние ключи
                entity.HasOne(r => r.Tour)
                      .WithMany()
                      .HasForeignKey(r => r.TourId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Индексы
                entity.HasIndex(r => new { r.TourId, r.UserId }).IsUnique();
                entity.HasIndex(r => r.TourId);
                entity.HasIndex(r => r.UserId);

                // Значения по умолчанию
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(r => r.IsVerified).HasDefaultValue(false);
            });

            // ✅ Конфигурация для WeatherCache
            modelBuilder.Entity<WeatherCache>(entity =>
            {
                entity.ToTable("weather_cache");

                entity.Property(w => w.Id).HasColumnName("id");
                entity.Property(w => w.Location).HasColumnName("location");
                entity.Property(w => w.ForecastDate).HasColumnName("forecast_date");
                entity.Property(w => w.TemperatureMin).HasColumnName("temperature_min");
                entity.Property(w => w.TemperatureMax).HasColumnName("temperature_max");
                entity.Property(w => w.Condition).HasColumnName("condition");
                entity.Property(w => w.Humidity).HasColumnName("humidity");
                entity.Property(w => w.WindSpeed).HasColumnName("wind_speed");
                entity.Property(w => w.Icon).HasColumnName("icon");
                entity.Property(w => w.CachedAt).HasColumnName("cached_at");
                entity.Property(w => w.ExpiresAt).HasColumnName("expires_at");

                // Индексы
                entity.HasIndex(w => new { w.Location, w.ForecastDate }).IsUnique();
                entity.HasIndex(w => w.ExpiresAt);

                // Значения по умолчанию
                entity.Property(w => w.CachedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}