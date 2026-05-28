using System.ComponentModel.DataAnnotations;


// TourManagementSystem/DTOs/EmailDto.cs
namespace TourManagementSystem.DTOs
{
    public class EmailSendDto
    {
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
    }

    public class TemplateEmailSendDto
    {
        public string ToEmail { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public Dictionary<string, string> TemplateData { get; set; } = new Dictionary<string, string>();
    }

    public class EmailTemplateDto
    {
        public int Id { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class EmailTemplateCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;
    }

    public class EmailTemplateUpdateDto
    {
        [MaxLength(100)]
        public string? TemplateName { get; set; }

        [MaxLength(200)]
        public string? Subject { get; set; }

        public string? Body { get; set; }

        public bool? IsActive { get; set; }
    }
}