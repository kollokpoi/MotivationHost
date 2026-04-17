using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Motivation.Data;

namespace Motivation.Models
{
    public class Department : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int? PortalId { get; set; }

        [ForeignKey("PortalId")]
        public BitrixPortal? Portal { get; set; }

        public int? ParentId { get; set; }
       
        [ForeignKey("ParentId")]
        public Department? Parent { get; set; }
        public List<Department>? Children { get; set; }

        [NotMapped]
        public int ManagerId { get;set; }
        public string Name { get; set; } = string.Empty;
        public decimal Budget { get; set; }

        [NotMapped]
        public decimal Expenses { get; set; }
    }
}
