using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDailyExpenseTracker.Models
{
    /// <summary>
    /// Represents an expense or income category (e.g., Food, Rent, Salary).
    /// Each user has their own set of categories.
    /// </summary>
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Icon { get; set; }   // Bootstrap icon class e.g. "bi-cart"

        [StringLength(7)]
        public string? Color { get; set; }  // Hex color e.g. "#FF5733"

        /// <summary>
        /// Expense = for expense categories, Income = for income categories, Both = applies to either.
        /// </summary>
        [Required]
        [StringLength(10)]
        public string Type { get; set; } = "Both"; // "Expense", "Income", "Both"

        public bool IsDefault { get; set; } = false;  // System-seeded categories
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Foreign key to user (null = system default visible to all)
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // Navigation
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<BudgetCategory> BudgetCategories { get; set; } = new List<BudgetCategory>();
    }
}
