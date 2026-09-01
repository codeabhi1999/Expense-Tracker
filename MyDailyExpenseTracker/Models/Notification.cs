using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyDailyExpenseTracker.Models
{
    /// <summary>
    /// User notification (budget warnings, overspending alerts, etc.)
    /// </summary>
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// "Info", "Warning", "Danger", "Success"
        /// </summary>
        [StringLength(20)]
        public string Type { get; set; } = "Info";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
    }
}
