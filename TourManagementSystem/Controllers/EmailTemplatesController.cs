// TourManagementSystem/Controllers/EmailTemplatesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourManagementSystem.Data;
using TourManagementSystem.DTOs;
using TourManagementSystem.Models;
using TourManagementSystem.Services.Interfaces;

namespace TourManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class EmailTemplatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailTemplatesController> _logger;

        public EmailTemplatesController(
            ApplicationDbContext context,
            ILogger<EmailTemplatesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/emailtemplates
        [HttpGet]
        public async Task<ActionResult<List<EmailTemplateDto>>> GetAllTemplates()
        {
            var templates = await _context.EmailTemplates
                .OrderBy(t => t.TemplateName)
                .ToListAsync();

            return templates.Select(MapToDto).ToList();
        }

        // GET: api/emailtemplates/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EmailTemplateDto>> GetTemplate(int id)
        {
            var template = await _context.EmailTemplates.FindAsync(id);

            if (template == null)
                return NotFound(new { message = "Шаблон не найден" });

            return MapToDto(template);
        }

        // POST: api/emailtemplates
        [HttpPost]
        public async Task<ActionResult<EmailTemplateDto>> CreateTemplate(EmailTemplateCreateDto createDto)
        {
            // Проверяем уникальность имени шаблона
            var existingTemplate = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.TemplateName == createDto.TemplateName);

            if (existingTemplate != null)
                return BadRequest(new { message = "Шаблон с таким именем уже существует" });

            var template = new EmailTemplate
            {
                TemplateName = createDto.TemplateName,
                Subject = createDto.Subject,
                Body = createDto.Body,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.EmailTemplates.Add(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Создан новый шаблон email: {template.TemplateName}");

            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, MapToDto(template));
        }

        // PUT: api/emailtemplates/5
        [HttpPut("{id}")]
        public async Task<ActionResult<EmailTemplateDto>> UpdateTemplate(int id, EmailTemplateUpdateDto updateDto)
        {
            var template = await _context.EmailTemplates.FindAsync(id);

            if (template == null)
                return NotFound(new { message = "Шаблон не найден" });

            if (!string.IsNullOrEmpty(updateDto.TemplateName))
                template.TemplateName = updateDto.TemplateName;

            if (!string.IsNullOrEmpty(updateDto.Subject))
                template.Subject = updateDto.Subject;

            if (!string.IsNullOrEmpty(updateDto.Body))
                template.Body = updateDto.Body;

            if (updateDto.IsActive.HasValue)
                template.IsActive = updateDto.IsActive.Value;

            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Обновлен шаблон email: {template.TemplateName}");

            return MapToDto(template);
        }

        // DELETE: api/emailtemplates/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTemplate(int id)
        {
            var template = await _context.EmailTemplates.FindAsync(id);

            if (template == null)
                return NotFound(new { message = "Шаблон не найден" });

            _context.EmailTemplates.Remove(template);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Удален шаблон email: {template.TemplateName}");

            return NoContent();
        }

        // PATCH: api/emailtemplates/5/toggle-status
        [HttpPatch("{id}/toggle-status")]
        public async Task<ActionResult> ToggleTemplateStatus(int id)
        {
            var template = await _context.EmailTemplates.FindAsync(id);

            if (template == null)
                return NotFound(new { message = "Шаблон не найден" });

            template.IsActive = !template.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var status = template.IsActive ? "активирован" : "деактивирован";
            _logger.LogInformation($"Шаблон {template.TemplateName} {status}");

            return Ok(new
            {
                message = $"Шаблон {status}",
                isActive = template.IsActive
            });
        }

        // GET: api/emailtemplates/names
        [HttpGet("names")]
        public async Task<ActionResult<List<string>>> GetTemplateNames()
        {
            var names = await _context.EmailTemplates
                .Where(t => t.IsActive)
                .Select(t => t.TemplateName)
                .OrderBy(n => n)
                .ToListAsync();

            return Ok(names);
        }

        private EmailTemplateDto MapToDto(EmailTemplate template)
        {
            return new EmailTemplateDto
            {
                Id = template.Id,
                TemplateName = template.TemplateName,
                Subject = template.Subject,
                Body = template.Body,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };
        }

        // Добавить в EmailTemplatesController
        [HttpPost("seed")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> SeedTemplates()
        {
            var templates = new List<EmailTemplate>
    {
        new()
        {
            TemplateName = "PaymentSuccess",
            Subject = "✅ Оплата прошла успешно — тур {{TourTitle}}",
            Body = @"
                <h2>Здравствуйте, {{UserName}}!</h2>
                <p>Оплата тура <strong>{{TourTitle}}</strong> успешно завершена.</p>
                
                <table style='border-collapse: collapse; width: 100%;'>
                    <tr style='background: #f5f5f5;'><td style='padding: 8px;'><strong>Сумма:</strong></td><td style='padding: 8px;'>{{Amount}} ₽</td></tr>
                    <tr><td style='padding: 8px;'><strong>Дата:</strong></td><td style='padding: 8px;'>{{PaymentDate}}</td></tr>
                    <tr style='background: #f5f5f5;'><td style='padding: 8px;'><strong>Номер транзакции:</strong></td><td style='padding: 8px;'>{{TransactionId}}</td></tr>
                    <tr><td style='padding: 8px;'><strong>Бронирование:</strong></td><td style='padding: 8px;'>{{BookingNumber}}</td></tr>
                </table>
                
                <p>Скачать чек можно в личном кабинете.</p>
                <p>Спасибо за путешествие с нами! 🎒</p>
            ",
            IsActive = true
        },
        new()
        {
            TemplateName = "PaymentFailed",
            Subject = "❌ Ошибка оплаты — тур {{TourTitle}}",
            Body = @"
                <h2>Здравствуйте, {{UserName}}!</h2>
                <p>К сожалению, платёж на сумму <strong>{{Amount}} ₽</strong> не прошёл.</p>
                <p><strong>Причина:</strong> {{ErrorMessage}}</p>
                
                <p>Вы можете повторить попытку оплаты в личном кабинете.</p>
                <p>Если у вас есть вопросы, свяжитесь с нами.</p>
            ",
            IsActive = true
        },
        new()
        {
            TemplateName = "BookingConfirmed",
            Subject = "🎉 Бронирование подтверждено — {{TourTitle}}",
            Body = @"
                <h2>Поздравляем, {{UserName}}!</h2>
                <p>Ваше бронирование тура <strong>{{TourTitle}}</strong> подтверждено!</p>
                
                <table style='border-collapse: collapse; width: 100%;'>
                    <tr style='background: #f5f5f5;'><td style='padding: 8px;'><strong>Номер брони:</strong></td><td style='padding: 8px;'>{{BookingNumber}}</td></tr>
                    <tr><td style='padding: 8px;'><strong>Дата начала:</strong></td><td style='padding: 8px;'>{{TourStartDate}}</td></tr>
                    <tr style='background: #f5f5f5;'><td style='padding: 8px;'><strong>Количество участников:</strong></td><td style='padding: 8px;'>{{Participants}}</td></tr>
                    <tr><td style='padding: 8px;'><strong>Место встречи:</strong></td><td style='padding: 8px;'>{{MeetingPoint}}</td></tr>
                </table>
                
                <p>Детали поездки доступны в вашем личном кабинете.</p>
                <p>Желаем отличного отдыха! 🌍</p>
            ",
            IsActive = true
        }
    };

            foreach (var template in templates)
            {
                var exists = await _context.EmailTemplates.AnyAsync(t => t.TemplateName == template.TemplateName);
                if (!exists)
                {
                    _context.EmailTemplates.Add(template);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Шаблоны успешно добавлены" });
        }
    }
}