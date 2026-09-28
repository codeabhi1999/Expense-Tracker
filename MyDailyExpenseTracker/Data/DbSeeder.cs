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
                ("Food",             "Expense", "bi-cup-hot-fill",            "#FF5722"),
                ("Grocery",          "Expense", "bi-basket-fill",             "#06B6D4"),
                ("Transportation",   "Expense", "bi-bus-front-fill",          "#0EA5E9"),
                ("Shopping",         "Expense", "bi-bag-fill",                "#10B981"),
                ("Bills",            "Expense", "bi-receipt-cutoff",          "#F59E0B"),
                ("Electricity",      "Expense", "bi-lightning-charge-fill",   "#EAB308"),
                ("Internet",         "Expense", "bi-wifi",                    "#06B6D4"),
                ("Mobile Recharge",  "Expense", "bi-phone-fill",              "#8B5CF6"),
                ("Rent",             "Expense", "bi-house-door-fill",         "#8B5CF6"),
                ("Medical",          "Expense", "bi-heart-pulse-fill",        "#EC4899"),
                ("Education",        "Expense", "bi-book-fill",               "#14B8A6"),
                ("Entertainment",    "Expense", "bi-film",                    "#F97316"),
                ("Travel",           "Expense", "bi-airplane-fill",           "#22C55E"),
                ("Fuel",             "Expense", "bi-fuel-pump-fill",          "#E11D48"),
                ("EMI",              "Expense", "bi-credit-card-2-front-fill","#64748B"),
                ("Insurance",        "Expense", "bi-shield-fill-check",       "#10B981"),
                ("Other",            "Expense", "bi-grid-fill",               "#64748B"),
                // Income categories
                ("Salary",           "Income",  "bi-wallet-fill",             "#10B981"),
                ("Freelancing",      "Income",  "bi-laptop",                  "#3B82F6"),
                ("Business",         "Income",  "bi-building-fill",           "#F59E0B"),
                ("Bonus",            "Income",  "bi-gift-fill",               "#EC4899"),
                ("Interest",         "Income",  "bi-bank",                    "#8B5CF6"),
                ("Investment",       "Income",  "bi-graph-up-arrow",          "#06B6D4"),
                ("Other Income",     "Income",  "bi-cash-coin",               "#10B981"),
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
