namespace MyDailyExpenseTracker.ViewModels
{
    /// <summary>
    /// All data needed to render the main dashboard page.
    /// </summary>
    public class DashboardViewModel
    {
        // ── Summary Cards ──────────────────────────────────────────────
        public decimal TodayExpense       { get; set; }
        public decimal TodayIncome        { get; set; }
        public decimal MonthExpense       { get; set; }
        public decimal MonthIncome        { get; set; }
        public decimal TotalBalance       { get; set; }   // All-time: income - expense
        public int     TotalTransactions  { get; set; }

        // ── Month-over-Month ────────────────────────────────────────────
        public decimal PrevMonthExpense   { get; set; }
        public decimal MonthSavings       => MonthIncome - MonthExpense;
        public decimal SavingsPercentage  =>
            MonthIncome > 0 ? Math.Round((MonthSavings / MonthIncome) * 100, 1) : 0;

        // ── Recent Transactions ─────────────────────────────────────────
        public List<TransactionListItem> RecentTransactions { get; set; } = new();

        // ── Chart Data ──────────────────────────────────────────────────
        /// <summary>Last 6 months expense trend: { "Jan": 5000, "Feb": 4500, ... }</summary>
        public Dictionary<string, decimal> MonthlyExpenseChart { get; set; } = new();

        /// <summary>Last 6 months income vs expense: { "Jan": { income: 10000, expense: 5000 }, ... }</summary>
        public Dictionary<string, MonthlyIncomeExpense> MonthlyIncomeVsExpenseChart { get; set; } = new();

        /// <summary>Category-wise expense: { "Food": 5000, "Rent": 8000, ... }</summary>
        public Dictionary<string, decimal> CategoryExpenseChart { get; set; } = new();

        /// <summary>Daily expense trend for current month: { "1": 300, "2": 0, "3": 1200, ... }</summary>
        public Dictionary<string, decimal> DailyExpenseTrend { get; set; } = new();

        /// <summary>Payment method breakdown: { "Cash": 3000, "UPI": 7000, ... }</summary>
        public Dictionary<string, decimal> PaymentMethodChart { get; set; } = new();

        // ── Budget ──────────────────────────────────────────────────────
        public decimal? MonthlyBudget            { get; set; }
        public decimal  BudgetUsedPercentage     => MonthlyBudget.HasValue && MonthlyBudget > 0
                                                    ? Math.Round((MonthExpense / MonthlyBudget.Value) * 100, 1)
                                                    : 0;
        public bool     IsBudgetWarning          => BudgetUsedPercentage >= 80 && BudgetUsedPercentage < 100;
        public bool     IsBudgetExceeded         => BudgetUsedPercentage >= 100;

        // ── Notifications ────────────────────────────────────────────────
        public int      UnreadNotificationCount  { get; set; }
    }

    public class MonthlyIncomeExpense
    {
        public decimal Income  { get; set; }
        public decimal Expense { get; set; }
    }

    /// <summary>
    /// Lightweight DTO used in transaction lists (dashboard, recent transactions).
    /// </summary>
    public class TransactionListItem
    {
        public int      TransactionId     { get; set; }
        public DateTime TransactionDate   { get; set; }
        public string   Description       { get; set; } = string.Empty;
        public string   CategoryName      { get; set; } = string.Empty;
        public string?  CategoryIcon      { get; set; }
        public string?  CategoryColor     { get; set; }
        public string   Type              { get; set; } = string.Empty;
        public decimal  Amount            { get; set; }
        public string?  PaymentMethodName { get; set; }
    }
}
