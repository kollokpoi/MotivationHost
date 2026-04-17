using System.ComponentModel.DataAnnotations;
using Motivation.Data;

namespace Motivation.Models
{
    public class TokenRequest : BaseEntity
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
