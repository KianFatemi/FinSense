using Microsoft.AspNetCore.Identity;    

namespace PersonalFinanceDashboard.Models
{
    public class FinancialAccount
    {
        public int ID { get; set; }
        public string? AccountName { get; set; }
        public string? AccountType { get; set; }
        public decimal CurrentBalance { get; set; }
        public string? UserID { get; set; }
        public IdentityUser? User { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();


    }
}
