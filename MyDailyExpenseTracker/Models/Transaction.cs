using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDailyExpenseTracker.Models
{
    /// <summary>
    /// Core entity representing a single financial transaction (Expense or Income).
    /// </summary>
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        /// <summary>
        /// "Expense" or "Income"
        /// </summary>
        [Required]
        [StringLength(10)]
        public string Type { get; set; } = "Expense";

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.Today;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Foreign keys
        [Required]
        public int CategoryId { get; set; }

        public int? PaymentMethodId { get; set; }

        public int? RecurringExpenseId { get; set; }  // Link to recurring if generated

        // Navigation properties
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;

        [ForeignKey("PaymentMethodId")]
        public PaymentMethod? PaymentMethod { get; set; }

        [ForeignKey("RecurringExpenseId")]
        public RecurringExpense? RecurringExpense { get; set; }
    }
}
