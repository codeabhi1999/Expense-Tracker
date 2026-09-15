namespace MyDailyExpenseTracker.ViewModels
{
    public class AiInsightsViewModel
    {
        public FinancialHealthScore HealthScore { get; set; } = new();
        public BurnRateForecast Forecast { get; set; } = new();
        public List<SpendingAnomaly> Anomalies { get; set; } = new();
        public List<AiSmartSuggestion> Suggestions { get; set; } = new();
        public List<CategorySpendingTrend> CategoryTrends { get; set; } = new();
        public string CurrencySymbol { get; set; } = "₹";
    }

    public class FinancialHealthScore
    {
        public int Score { get; set; } // 0 to 100
        public string Grade { get; set; } = "B"; // A+, A, B, C, D
        public string StatusBadge { get; set; } = "Good";
        public string Title { get; set; } = "Stable Financial Health";
        public string Summary { get; set; } = string.Empty;
        public string AccentColor { get; set; } = "#00E676"; // green / cyan / warning / danger

        // 5 Pillars (each 0 - 100)
        public int SavingsRatioScore { get; set; }
        public int BudgetDisciplineScore { get; set; }
        public int SpendingStabilityScore { get; set; }
        public int EssentialBurdenScore { get; set; }
        public int CategoryBalanceScore { get; set; }

        public List<string> KeyStrengths { get; set; } = new();
        public List<string> ActionableTips { get; set; } = new();
    }

    public class BurnRateForecast
    {
        public decimal DailyBurnRate { get; set; }
        public decimal SafeDailyBudget { get; set; }
        public decimal MonthToDateExpense { get; set; }
        public decimal ProjectedMonthEndExpense { get; set; }
        public decimal? MonthlyBudget { get; set; }
        public int DaysElapsed { get; set; }
        public int DaysRemaining { get; set; }
        public decimal ProjectedBalance { get; set; } // surplus or deficit
        public bool WillExceedBudget { get; set; }
        public int DaysUntilBudgetDepletion { get; set; }
        public string PaceStatus { get; set; } = "On Track"; // "On Track", "Accelerating", "Critical"
    }

    public class SpendingAnomaly
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = "bi-tag";
        public decimal Amount { get; set; }
        public decimal BaselineAmount { get; set; }
        public double PercentageDiff { get; set; }
        public string Severity { get; set; } = "warning"; // "info", "warning", "danger"
        public string RecommendedAction { get; set; } = string.Empty;
    }

    public class AiSmartSuggestion
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-lightbulb-fill";
        public string ImpactBadge { get; set; } = "Medium Impact";
        public string ActionType { get; set; } = "view"; // "budget", "recurring", "filter", "general"
        public string ActionUrl { get; set; } = string.Empty;
    }

    public class CategorySpendingTrend
    {
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = "bi-tag";
        public string CategoryColor { get; set; } = "#6366F1";
        public decimal CurrentMonthAmount { get; set; }
        public decimal PreviousMonthAmount { get; set; }
        public double GrowthPercentage { get; set; }
        public bool IsHigher => GrowthPercentage > 0;
    }

    public class AiParsedTransaction
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
        public string RawInput { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = "Expense"; // Expense or Income
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.Today;

        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryIcon { get; set; } = "bi-tag";

        public int? PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;

        public double Confidence { get; set; } = 0.95;
    }

    public class AiChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? Context { get; set; }
    }

    public class AiChatResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<string> FollowUpSuggestions { get; set; } = new();
        public object? StructuredData { get; set; }
    }
}
