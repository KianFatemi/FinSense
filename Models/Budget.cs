using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PersonalFinanceDashboard.Models
{
    public class Budget
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        public string UserID { get; set; }
        public IdentityUser User { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; }

    }
}
