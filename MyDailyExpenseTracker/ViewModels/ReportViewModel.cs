using System.ComponentModel.DataAnnotations;

namespace MyDailyExpenseTracker.ViewModels
{
    /// <summary>
    /// ViewModel for the Monthly Reports page.
    /// </summary>
    public class ReportViewModel
    {
        // Filter
        [Range(1, 12)]
        public int Month { get; set; } = DateTime.Now.Month;
        [Range(2000, 2100)]
        public int Year  { get; set; } = DateTime.Now.Year;

        // Summary stats
        public decimal TotalIncome         { get; set; }
        public decimal TotalExpense        { get; set; }
        public decimal TotalSavings        => TotalIncome - TotalExpense;
        public decimal HighestExpense      { get; set; }
        public decimal LowestExpense       { get; set; }
        public decimal AverageDailyExpense { get; set; }
        public int     TotalTransactions   { get; set; }
        public decimal SavingsPercentage   =>
            TotalIncome > 0 ? Math.Round((TotalSavings / TotalIncome) * 100, 1) : 0;

        // Category breakdown
        public List<CategoryBreakdown> CategoryBreakdowns { get; set; } = new();

        // Chart data
        public Dictionary<string, decimal> CategoryExpenseChart { get; set; } = new();
        public Dictionary<string, decimal> DailyExpenseTrend    { get; set; } = new();
        public Dictionary<string, decimal> PaymentMethodChart   { get; set; } = new();

        // All transactions in selected month (for the table)
        public List<TransactionListItem> Transactions { get; set; } = new();

        // Year/Month dropdowns
        public List<int> AvailableYears  { get; set; } = new();
    }

    public class CategoryBreakdown
    {
        public string  CategoryName  { get; set; } = string.Empty;
        public string? CategoryIcon  { get; set; }
        public string? CategoryColor { get; set; }
        public decimal Amount        { get; set; }
        public int     Count         { get; set; }
        public decimal Percentage    { get; set; }  // % of total expense
    }
}
