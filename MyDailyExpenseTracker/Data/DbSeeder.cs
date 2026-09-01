using Microsoft.AspNetCore.Identity;
using MyDailyExpenseTracker.Models;

namespace MyDailyExpenseTracker.Data
{
    /// <summary>
    /// Seeds the database with default categories and payment methods.
    /// This runs on app startup and is idempotent (safe to run multiple times).
    /// No fake user transactions are created — only master data.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // ── Seed Roles ─────────────────────────────────────────────
            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ── Seed Default Categories ───────────────────────────────
            // UserId = null → system defaults visible to all users
            var defaultCategories = new List<(string Name, string Type, string Icon, string Color)>
            {
                // Expense categories
                ("Food",             "Expense", "bi-cup-hot",         "#FF6B6B"),
                ("Grocery",          "Expense", "bi-basket",          "#4ECDC4"),
                ("Transportation",   "Expense", "bi-bus-front",       "#45B7D1"),
                ("Shopping",         "Expense", "bi-bag",             "#96CEB4"),
                ("Bills",            "Expense", "bi-file-text",       "#FFEAA7"),
                ("Electricity",      "Expense", "bi-lightning",       "#FFA07A"),
                ("Internet",         "Expense", "bi-wifi",            "#98D8C8"),
                ("Mobile Recharge",  "Expense", "bi-phone",           "#B2EBF2"),
                ("Rent",             "Expense", "bi-house",           "#F8BBD0"),
                ("Medical",          "Expense", "bi-heart-pulse",     "#CE93D8"),
                ("Education",        "Expense", "bi-book",            "#80CBC4"),
                ("Entertainment",    "Expense", "bi-camera-video",    "#FFB74D"),
                ("Travel",           "Expense", "bi-airplane",        "#81C784"),
                ("Fuel",             "Expense", "bi-fuel-pump",       "#FF8A65"),
                ("EMI",              "Expense", "bi-credit-card",     "#90A4AE"),
                ("Insurance",        "Expense", "bi-shield-check",    "#A5D6A7"),
                ("Other",            "Expense", "bi-three-dots",      "#BDBDBD"),
                // Income categories
                ("Salary",           "Income",  "bi-wallet2",         "#66BB6A"),
                ("Freelancing",      "Income",  "bi-laptop",          "#42A5F5"),
                ("Business",         "Income",  "bi-building",        "#FFA726"),
                ("Bonus",            "Income",  "bi-gift",            "#EC407A"),
                ("Interest",         "Income",  "bi-bank",            "#AB47BC"),
                ("Investment",       "Income",  "bi-graph-up-arrow",  "#26C6DA"),
                ("Other Income",     "Income",  "bi-plus-circle",     "#BDBDBD"),
            };

            foreach (var (name, type, icon, color) in defaultCategories)
            {
                if (!context.Categories.Any(c => c.Name == name && c.UserId == null))
                {
                    context.Categories.Add(new Category
                    {
                        Name      = name,
                        Type      = type,
                        Icon      = icon,
                        Color     = color,
                        IsDefault = true,
                        IsActive  = true,
                        UserId    = null   // System default
                    });
                }
            }

            // ── Seed Default Payment Methods ──────────────────────────
            var defaultPaymentMethods = new List<(string Name, string Icon)>
            {
                ("Cash",          "bi-cash"),
                ("UPI",           "bi-phone"),
                ("Credit Card",   "bi-credit-card"),
                ("Debit Card",    "bi-credit-card-2-front"),
                ("Net Banking",   "bi-bank"),
                ("Bank Transfer", "bi-arrow-left-right"),
                ("Other",         "bi-three-dots"),
            };

            foreach (var (name, icon) in defaultPaymentMethods)
            {
                if (!context.PaymentMethods.Any(pm => pm.Name == name && pm.UserId == null))
                {
                    context.PaymentMethods.Add(new PaymentMethod
                    {
                        Name      = name,
                        Icon      = icon,
                        IsDefault = true,
                        IsActive  = true,
                        UserId    = null   // System default
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
