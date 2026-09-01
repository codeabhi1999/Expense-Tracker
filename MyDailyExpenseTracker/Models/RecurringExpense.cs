using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDailyExpenseTracker.Models
{
    /// <summary>
    /// Defines a recurring expense/income rule (e.g., monthly rent).
    /// Actual transactions are generated from this template.
    /// </summary>
    public class RecurringExpense
    {
        [Key]
        public int RecurringExpenseId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(10)]
        public string Type { get; set; } = "Expense"; // "Expense" or "Income"

        [Required]
        public int CategoryId { get; set; }

        public int? PaymentMethodId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// How often this expense recurs: Daily, Weekly, Monthly, Yearly
        /// </summary>
        [Required]
        [StringLength(10)]
        public string Frequency { get; set; } = "Monthly";

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Tracks the last date a transaction was generated, to avoid duplicates.
        /// </summary>
        public DateTime? LastGeneratedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;

        [ForeignKey("PaymentMethodId")]
        public PaymentMethod? PaymentMethod { get; set; }

        public ICollection<Transaction> GeneratedTransactions { get; set; } = new List<Transaction>();
    }
}
