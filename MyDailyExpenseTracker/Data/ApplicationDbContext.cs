using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyDailyExpenseTracker.Models;

namespace MyDailyExpenseTracker.Data
{
    /// <summary>
    /// Main database context for the application.
    /// Inherits from IdentityDbContext so that ASP.NET Core Identity tables
    /// (AspNetUsers, AspNetRoles, etc.) are automatically managed.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- Application DbSets ---
        public DbSet<Category> Categories { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<BudgetCategory> BudgetCategories { get; set; }
        public DbSet<RecurringExpense> RecurringExpenses { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── Category ──────────────────────────────────────────────
            builder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => new { c.UserId, c.Name });

                // A category can belong to a user, or be a system default (null UserId)
                entity.HasOne(c => c.User)
                    .WithMany(u => u.Categories)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false);
            });

            // ── PaymentMethod ─────────────────────────────────────────
            builder.Entity<PaymentMethod>(entity =>
            {
                entity.HasIndex(pm => new { pm.UserId, pm.Name });

                entity.HasOne(pm => pm.User)
                    .WithMany(u => u.PaymentMethods)
                    .HasForeignKey(pm => pm.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false);
            });

            // ── Transaction ───────────────────────────────────────────
            builder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(t => new { t.UserId, t.TransactionDate });
                entity.HasIndex(t => t.Type);

                entity.HasOne(t => t.User)
                    .WithMany(u => u.Transactions)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Restrict delete: prevent deleting a category that has transactions
                entity.HasOne(t => t.Category)
                    .WithMany(c => c.Transactions)
                    .HasForeignKey(t => t.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.PaymentMethod)
                    .WithMany(pm => pm.Transactions)
                    .HasForeignKey(t => t.PaymentMethodId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);

                entity.HasOne(t => t.RecurringExpense)
                    .WithMany(re => re.GeneratedTransactions)
                    .HasForeignKey(t => t.RecurringExpenseId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);
            });

            // ── Budget ────────────────────────────────────────────────
            builder.Entity<Budget>(entity =>
            {
                // Each user can only have one budget per month/year
                entity.HasIndex(b => new { b.UserId, b.Month, b.Year }).IsUnique();

                entity.HasOne(b => b.User)
                    .WithMany(u => u.Budgets)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── BudgetCategory ────────────────────────────────────────
            builder.Entity<BudgetCategory>(entity =>
            {
                entity.HasIndex(bc => new { bc.BudgetId, bc.CategoryId }).IsUnique();

                entity.HasOne(bc => bc.Budget)
                    .WithMany(b => b.BudgetCategories)
                    .HasForeignKey(bc => bc.BudgetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(bc => bc.Category)
                    .WithMany(c => c.BudgetCategories)
                    .HasForeignKey(bc => bc.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── RecurringExpense ──────────────────────────────────────
            builder.Entity<RecurringExpense>(entity =>
            {
                entity.HasOne(re => re.User)
                    .WithMany(u => u.RecurringExpenses)
                    .HasForeignKey(re => re.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(re => re.Category)
                    .WithMany()
                    .HasForeignKey(re => re.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(re => re.PaymentMethod)
                    .WithMany()
                    .HasForeignKey(re => re.PaymentMethodId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);
            });

            // ── Notification ──────────────────────────────────────────
            builder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => new { n.UserId, n.IsRead });

                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
