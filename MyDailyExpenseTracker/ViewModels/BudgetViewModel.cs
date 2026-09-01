using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyDailyExpenseTracker.ViewModels
{
    /// <summary>
    /// ViewModel for the Budget management page (set monthly + category budgets).
    /// </summary>
    public class BudgetViewModel
    {
        public int BudgetId { get; set; }

        [Required(ErrorMessage = "Please enter a total monthly budget.")]
        [Range(1, 9999999.99, ErrorMessage = "Budget must be between ₹1 and ₹99,99,999.99.")]
        [Display(Name = "Total Monthly Budget (₹)")]
        public decimal TotalBudget { get; set; }

        [Required]
        [Range(1, 12)]
        [Display(Name = "Month")]
        public int Month { get; set; } = DateTime.Now.Month;

        [Required]
        [Range(2000, 2100)]
        [Display(Name = "Year")]
        public int Year { get; set; } = DateTime.Now.Year;

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Per-category budget entries
        public List<BudgetCategoryItem> CategoryBudgets { get; set; } = new();

        // Stats (populated when viewing existing budget)
        public decimal TotalSpent        { get; set; }
        public decimal RemainingBudget   => TotalBudget - TotalSpent;
        public decimal UsedPercentage    => TotalBudget > 0 ? Math.Round((TotalSpent / TotalBudget) * 100, 1) : 0;
        public bool    IsWarning         => UsedPercentage >= 80 && UsedPercentage < 100;
        public bool    IsExceeded        => UsedPercentage >= 100;

        // Dropdowns
        public List<SelectListItem> Months { get; set; } = new();
        public List<SelectListItem> Years  { get; set; } = new();
    }

    public class BudgetCategoryItem
    {
        public int    BudgetCategoryId { get; set; }
        public int    CategoryId       { get; set; }
        public string CategoryName     { get; set; } = string.Empty;
        public string? CategoryIcon    { get; set; }
        public string? CategoryColor   { get; set; }

        [Range(0, 9999999.99)]
        public decimal BudgetAmount    { get; set; }

        public decimal SpentAmount     { get; set; }
        public decimal Remaining       => BudgetAmount - SpentAmount;
        public decimal UsedPercentage  => BudgetAmount > 0 ? Math.Round((SpentAmount / BudgetAmount) * 100, 1) : 0;
        public bool    IsWarning       => BudgetAmount > 0 && UsedPercentage >= 80 && UsedPercentage < 100;
        public bool    IsExceeded      => BudgetAmount > 0 && UsedPercentage >= 100;
    }
}
