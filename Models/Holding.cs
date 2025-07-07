using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalFinanceDashboard.Models
{
    public class Holding
    {
        public int ID { get; set; }
        public int FinancialAccountId { get; set; }
        public FinancialAccount? FinancialAccount { get; set; }
        public int SecurityId { get; set; }
        public Security? Security { get; set; }

        [Column(TypeName = "decimal(18, 8)")] 
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? CostBasis { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal InstitutionValue { get; set; }


    }
}
