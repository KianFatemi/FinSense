using PersonalFinanceDashboard.Models;

namespace PersonalFinanceDashboard.Views.ViewModels
{
    public class DashboardDataViewModel
    {
        public decimal TotalBalance { get; set; }
        public decimal MainAccountBalance { get; set; }
        public decimal SavingsBalance { get; set; }
        public List<TransactionViewModel>? RecentTransactions { get; set; }
        public List<BalanceHistoryPoint> AccountBalanceHistory {  get; set; } = new();
        public decimal InvestmentsValue { get; set; }
        public List<InvestmentHoldingViewModel> InvestmentHoldings { get; set; } = new();
    }

    public class TransactionViewModel
    {
        public string? Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
    }

    public class BalanceHistoryPoint
    {
        public string? Date { get; set; }
        public decimal Balance { get; set; }
    }

    public class InvestmentHoldingViewModel
    {
        public string? TickerSymbol { get; set; }
        public decimal TotalValue { get; set; }
    }
}
