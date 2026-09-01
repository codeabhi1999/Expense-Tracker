using System.ComponentModel.DataAnnotations;

namespace MyDailyExpenseTracker.ViewModels
{
    /// <summary>
    /// ViewModel for the Add/Edit Transaction form.
    /// Used for both Expense and Income transactions.
    /// </summary>
    public class TransactionViewModel
    {
        public int TransactionId { get; set; }

        [Required(ErrorMessage = "Please select a transaction type.")]
        [Display(Name = "Transaction Type")]
        public string Type { get; set; } = "Expense";  // "Expense" or "Income"

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 9999999.99, ErrorMessage = "Amount must be between ₹0.01 and ₹99,99,999.99.")]
        [Display(Name = "Amount (₹)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Payment Method")]
        public int? PaymentMethodId { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        [Display(Name = "Notes (optional)")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime TransactionDate { get; set; } = DateTime.Today;

        // Read-only display fields (populated when viewing/editing)
        public string? CategoryName { get; set; }
        public string? PaymentMethodName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        // Dropdown options
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Categories { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> PaymentMethods { get; set; } = new();
    }
}
