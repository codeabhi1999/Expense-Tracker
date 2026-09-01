using System.ComponentModel.DataAnnotations;

namespace MyDailyExpenseTracker.ViewModels
{
    // ─── Account ViewModels ─────────────────────────────────────────────────────

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ─── Profile ─────────────────────────────────────────────────────────────────
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }

        // Settings
        [Display(Name = "Currency")]
        public string Currency { get; set; } = "INR";

        [Display(Name = "Date Format")]
        public string DateFormat { get; set; } = "dd-MM-yyyy";

        [Display(Name = "Theme")]
        public string Theme { get; set; } = "light";

        public string FullName => $"{FirstName} {LastName}";
        public DateTime MemberSince { get; set; }
        public int TotalTransactions { get; set; }
    }

    // ─── Category ─────────────────────────────────────────────────────────────────
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Icon (Bootstrap icon class)")]
        public string? Icon { get; set; }

        [StringLength(7)]
        [Display(Name = "Color (hex)")]
        public string? Color { get; set; }

        [Required]
        [Display(Name = "Type")]
        public string Type { get; set; } = "Both";

        public bool IsDefault { get; set; }
        public int  TransactionCount { get; set; }  // For safe-delete check
    }

    // ─── Payment Method ──────────────────────────────────────────────────────────
    public class PaymentMethodViewModel
    {
        public int PaymentMethodId { get; set; }

        [Required(ErrorMessage = "Payment method name is required.")]
        [StringLength(100)]
        [Display(Name = "Payment Method Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Icon (Bootstrap icon class)")]
        public string? Icon { get; set; }

        public bool IsDefault { get; set; }
        public int  TransactionCount { get; set; }
    }

    // ─── Recurring Expense ────────────────────────────────────────────────────────
    public class RecurringExpenseViewModel
    {
        public int RecurringExpenseId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 9999999.99)]
        [Display(Name = "Amount (₹)")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Type")]
        public string Type { get; set; } = "Expense";

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Payment Method")]
        public int? PaymentMethodId { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Required]
        [Display(Name = "Frequency")]
        public string Frequency { get; set; } = "Monthly";

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "End Date (optional)")]
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? LastGeneratedDate { get; set; }
        public string? CategoryName { get; set; }
        public string? PaymentMethodName { get; set; }

        // Dropdowns
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Categories     { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> PaymentMethods { get; set; } = new();
    }
}
