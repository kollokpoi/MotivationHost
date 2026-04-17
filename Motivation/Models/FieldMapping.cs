using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTrackingSystem.Models
{
    /// <summary>
    /// Модель настройки маппинга полей между локальной системой и Bitrix24
    /// </summary>
    public class FieldMapping
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PortalId { get; set; }

        [ForeignKey(nameof(PortalId))]
        public virtual BitrixPortal? Portal { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty; // "Department", "Employee", "Task"

        [Required]
        [MaxLength(100)]
        public string LocalFieldName { get; set; } = string.Empty; // Имя свойства в C# модели (например, "Cost", "Name")

        [Required]
        [MaxLength(100)]
        public string BitrixCode { get; set; } = string.Empty; // Код поля в Битрикс24 (например, "BUDGET", "UF_CRM_123")

        [MaxLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Тип преобразования данных (на будущее для сложной логики)
        /// </summary>
        public string MappingType { get; set; } = "Direct"; // Direct, ConvertToDecimal, Concatenate

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
