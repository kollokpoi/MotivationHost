using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public enum EmployeeStatus
    {
        AtWork,
        Abscent,
        AtBreak,
        WorkComplete,
    }

    public class Employee : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        public int PositionId { get; set; }

        [ForeignKey("PositionId")]
        public Position? Position { get; set; }

        public int QualificationId { get; set; }

        [ForeignKey("QualificationId")]
        public Qualification? Qualification { get; set; }

        public int RankId { get; set; }

        [ForeignKey("RankId")]
        public Rank? Rank { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Photo { get; set; } = "/images/profile.png";
        public bool IsManager { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Abscent;

        public int BitrixUserId { get; set; }

        [NotMapped]
        public string Password { get; set; } = string.Empty;

        [NotMapped]
        public string PushToken { get; set; } = string.Empty;

        public string GetFullName()
        {
            return $"{LastName} {FirstName} {MiddleName}";
        }

        public string GetShortName()
        {
            return $"{LastName} {FirstName.FirstOrDefault()}. {MiddleName.FirstOrDefault()}.";
        }
    }
}
