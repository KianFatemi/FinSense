namespace PersonalFinanceDashboard.Models
{
    public class Transaction
    {
        public int ID { get; set; }
        public string? Description{ get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Category { get; set; }
        public int FinancialAccountId { get; set; }
        public FinancialAccount? FinancialAccount { get; set; }
    }
}
