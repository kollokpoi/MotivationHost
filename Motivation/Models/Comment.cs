using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public class Comment : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public Employee? Author { get; set; }

        public int EmployeeTaskId { get; set; }

        [ForeignKey("EmployeeTaskId")]
        public EmployeeTask? EmployeeTask { get; set; }

        public string Text { get; set; } = string.Empty;
        public string Photo { get; set; } = string.Empty;
    }
}
