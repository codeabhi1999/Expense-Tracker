using System.ComponentModel.DataAnnotations;

namespace MyDailyExpenseTracker.ViewModels
{
    /// <summary>
    /// ViewModel for the transaction list page with search and filter parameters.
    /// </summary>
    public class TransactionFilterViewModel
    {
        // Filter inputs
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Payment Method")]
        public int? PaymentMethodId { get; set; }

        [Display(Name = "Type")]
        public string? Type { get; set; }   // "Expense", "Income", or null = all

        [Display(Name = "Min Amount")]
        [Range(0, double.MaxValue)]
        public decimal? MinAmount { get; set; }

        [Display(Name = "Max Amount")]
        [Range(0, double.MaxValue)]
        public decimal? MaxAmount { get; set; }

        [Display(Name = "Search")]
        [StringLength(200)]
        public string? SearchText { get; set; }

        // Pagination
        public int Page     { get; set; } = 1;
        public int PageSize { get; set; } = 15;

        // Sorting
        public string SortBy    { get; set; } = "Date";
        public string SortOrder { get; set; } = "desc";

        // Results
        public List<TransactionListItem>  Transactions { get; set; } = new();
        public int TotalCount    { get; set; }
        public int TotalPages    => (int)Math.Ceiling((double)TotalCount / PageSize);

        // Summary of filtered results
        public decimal FilteredTotalExpense { get; set; }
        public decimal FilteredTotalIncome  { get; set; }
        public decimal FilteredBalance      => FilteredTotalIncome - FilteredTotalExpense;

        // Dropdown options
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Categories     { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> PaymentMethods { get; set; } = new();
    }
}
