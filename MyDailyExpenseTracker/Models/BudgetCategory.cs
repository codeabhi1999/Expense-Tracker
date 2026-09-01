using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDailyExpenseTracker.Models
{
    /// <summary>
    /// Per-category budget limit within an overall monthly Budget.
    /// </summary>
    public class BudgetCategory
    {
        [Key]
        public int BudgetCategoryId { get; set; }

        [Required]
        public int BudgetId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Budget amount must be greater than 0.")]
        public decimal Amount { get; set; }

        // Navigation
        [ForeignKey("BudgetId")]
        public Budget Budget { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;
    }
}
