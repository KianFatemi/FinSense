using PersonalFinanceDashboard.Models;

namespace PersonalFinanceDashboard.Views.ViewModels
{
    public class DashboardDataViewModel
    {
        public decimal TotalBalance { get; set; }
        public decimal MainAccountBalance { get; set; }
        public decimal SavingsBalance { get; set; }
        public List<Transaction> RecentTransactions { get; set; } = new();
        public List<BalanceHistoryPoint> AccountBalanceHistory {  get; set; } = new();
    }

    public class BalanceHistoryPoint
    {
        public string? Date { get; set; }
        public decimal Balance { get; set; }
    }
}
