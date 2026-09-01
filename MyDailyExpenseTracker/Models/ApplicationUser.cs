using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MyDailyExpenseTracker.Models
{
    /// <summary>
    /// Extends the default Identity user with additional profile fields.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(10)]
        public string Currency { get; set; } = "INR";

        [StringLength(20)]
        public string DateFormat { get; set; } = "dd-MM-yyyy";

        [StringLength(10)]
        public string Theme { get; set; } = "light";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
        public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
        public ICollection<RecurringExpense> RecurringExpenses { get; set; } = new List<RecurringExpense>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        // Computed property
        public string FullName => $"{FirstName} {LastName}";
    }
}
