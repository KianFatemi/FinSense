namespace PersonalFinanceDashboard.Views.ViewModels
{
    public class BudgetViewModel
    {
        public int BudgetId { get; set; }
        public string Category { get; set; }
        public decimal BudgetAmount { get; set; }
        public decimal ActualSpending { get; set; }

        // Calculated property to get the remaining amount
        public decimal RemainingAmount => BudgetAmount - ActualSpending;

        // Calculated property to get the progress percentage for a progress bar
        public double ProgressPercentage => BudgetAmount > 0 ? (double)(ActualSpending / BudgetAmount) * 100 : 0;
    }
}
