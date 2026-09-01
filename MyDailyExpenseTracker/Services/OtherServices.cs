using Microsoft.EntityFrameworkCore;
using MyDailyExpenseTracker.Data;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Services
{
    /// <summary>
    /// Handles budget creation and retrieval with per-category breakdown.
    /// </summary>
    public class BudgetService : IBudgetService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICategoryService _categoryService;

        public BudgetService(ApplicationDbContext db, ICategoryService categoryService)
        {
            _db              = db;
            _categoryService = categoryService;
        }

        public async Task<BudgetViewModel> GetOrCreateBudgetAsync(string userId, int month, int year)
        {
            var budget = await _db.Budgets
                .AsNoTracking()
                .Include(b => b.BudgetCategories)
                    .ThenInclude(bc => bc.Category)
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == month && b.Year == year);

            // Get all expense categories for this user
            var allCategories = await _categoryService.GetCategoriesAsync(userId);
            var expenseCategories = allCategories.Where(c => c.Type == "Expense" || c.Type == "Both").ToList();

            // Get actual spending per category for the month
            var monthStart = new DateTime(year, month, 1);
            var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

            var spending = await _db.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.Type == "Expense"
                         && t.TransactionDate >= monthStart && t.TransactionDate <= monthEnd)
                .GroupBy(t => t.CategoryId)
                .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
                .ToDictionaryAsync(g => g.CategoryId, g => g.Total);

            var totalSpent = spending.Values.Sum();

            // Build category budget items
            var categoryBudgets = expenseCategories.Select(cat =>
            {
                var budgetCat = budget?.BudgetCategories.FirstOrDefault(bc => bc.CategoryId == cat.CategoryId);
                return new BudgetCategoryItem
                {
                    BudgetCategoryId = budgetCat?.BudgetCategoryId ?? 0,
                    CategoryId       = cat.CategoryId,
                    CategoryName     = cat.Name,
                    CategoryIcon     = cat.Icon,
                    CategoryColor    = cat.Color,
                    BudgetAmount     = budgetCat?.Amount ?? 0,
                    SpentAmount      = spending.GetValueOrDefault(cat.CategoryId)
                };
            }).ToList();

            return new BudgetViewModel
            {
                BudgetId        = budget?.BudgetId ?? 0,
                TotalBudget     = budget?.TotalBudget ?? 0,
                Month           = month,
                Year            = year,
                Notes           = budget?.Notes,
                TotalSpent      = totalSpent,
                CategoryBudgets = categoryBudgets,
                Months          = Enumerable.Range(1, 12).Select(m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value    = m.ToString(),
                    Text     = new DateTime(2000, m, 1).ToString("MMMM"),
                    Selected = m == month
                }).ToList(),
                Years = Enumerable.Range(DateTime.Now.Year - 3, 8).Select(y => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value    = y.ToString(),
                    Text     = y.ToString(),
                    Selected = y == year
                }).ToList()
            };
        }

        public async Task<bool> SaveBudgetAsync(string userId, BudgetViewModel vm)
        {
            var budget = await _db.Budgets
                .Include(b => b.BudgetCategories)
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == vm.Month && b.Year == vm.Year);

            if (budget == null)
            {
                budget = new Budget
                {
                    UserId      = userId,
                    Month       = vm.Month,
                    Year        = vm.Year,
                    CreatedDate = DateTime.UtcNow
                };
                _db.Budgets.Add(budget);
            }

            budget.TotalBudget  = vm.TotalBudget;
            budget.Notes        = vm.Notes;
            budget.UpdatedDate  = DateTime.UtcNow;

            // Update / insert category budgets
            foreach (var item in vm.CategoryBudgets)
            {
                if (item.BudgetAmount <= 0) continue;  // Skip empty entries

                var existing = budget.BudgetCategories.FirstOrDefault(bc => bc.CategoryId == item.CategoryId);
                if (existing != null)
                    existing.Amount = item.BudgetAmount;
                else
                    budget.BudgetCategories.Add(new BudgetCategory
                    {
                        CategoryId = item.CategoryId,
                        Amount     = item.BudgetAmount
                    });
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<decimal?> GetMonthlyBudgetAmountAsync(string userId, int month, int year)
        {
            var budget = await _db.Budgets
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == month && b.Year == year);
            return budget?.TotalBudget;
        }
    }

    /// <summary>
    /// Generates monthly expense reports with statistics and breakdowns.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db) => _db = db;

        public async Task<ReportViewModel> GetMonthlyReportAsync(string userId, int month, int year)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd   = monthStart.AddMonths(1).AddDays(-1);
            int daysInMonth = DateTime.DaysInMonth(year, month);

            var transactions = await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.PaymentMethod)
                .Where(t => t.UserId == userId
                         && t.TransactionDate >= monthStart
                         && t.TransactionDate <= monthEnd)
                .ToListAsync();

            var expenses = transactions.Where(t => t.Type == "Expense").ToList();
            var incomes  = transactions.Where(t => t.Type == "Income").ToList();

            var totalExpense = expenses.Sum(t => t.Amount);
            var totalIncome  = incomes.Sum(t => t.Amount);

            // Category breakdown
            var categoryBreakdowns = expenses
                .GroupBy(t => new { t.Category.Name, t.Category.Icon, t.Category.Color })
                .Select(g => new CategoryBreakdown
                {
                    CategoryName  = g.Key.Name,
                    CategoryIcon  = g.Key.Icon,
                    CategoryColor = g.Key.Color,
                    Amount        = g.Sum(t => t.Amount),
                    Count         = g.Count(),
                    Percentage    = totalExpense > 0 ? Math.Round(g.Sum(t => t.Amount) / totalExpense * 100, 1) : 0
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // Daily expense trend
            var dailyTrend = new Dictionary<string, decimal>();
            for (int d = 1; d <= daysInMonth; d++)
                dailyTrend[d.ToString()] = expenses.Where(t => t.TransactionDate.Day == d).Sum(t => t.Amount);

            // Payment method chart
            var paymentChart = expenses
                .Where(t => t.PaymentMethod != null)
                .GroupBy(t => t.PaymentMethod!.Name)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            // Available years (from first transaction to current year)
            var firstTxYear = await _db.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .MinAsync(t => (DateTime?)t.TransactionDate);

            var startYear = firstTxYear?.Year ?? DateTime.Now.Year;
            var endYear   = DateTime.Now.Year;

            return new ReportViewModel
            {
                Month              = month,
                Year               = year,
                TotalIncome        = totalIncome,
                TotalExpense       = totalExpense,
                HighestExpense     = expenses.Any() ? expenses.Max(t => t.Amount) : 0,
                LowestExpense      = expenses.Any() ? expenses.Min(t => t.Amount) : 0,
                AverageDailyExpense = daysInMonth > 0 ? Math.Round(totalExpense / daysInMonth, 2) : 0,
                TotalTransactions  = transactions.Count,
                CategoryBreakdowns = categoryBreakdowns,
                CategoryExpenseChart = categoryBreakdowns.Take(8).ToDictionary(c => c.CategoryName, c => c.Amount),
                DailyExpenseTrend  = dailyTrend,
                PaymentMethodChart = paymentChart,
                Transactions       = transactions
                    .OrderByDescending(t => t.TransactionDate)
                    .Select(t => new TransactionListItem
                    {
                        TransactionId     = t.TransactionId,
                        TransactionDate   = t.TransactionDate,
                        Description       = t.Description,
                        CategoryName      = t.Category.Name,
                        CategoryIcon      = t.Category.Icon,
                        CategoryColor     = t.Category.Color,
                        Type              = t.Type,
                        Amount            = t.Amount,
                        PaymentMethodName = t.PaymentMethod?.Name
                    }).ToList(),
                AvailableYears = Enumerable.Range(startYear, endYear - startYear + 1).Reverse().ToList()
            };
        }
    }

    /// <summary>
    /// Creates and manages user notifications for budget warnings.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;

        public NotificationService(ApplicationDbContext db) => _db = db;

        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            return await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedDate)
                .Take(20)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var n = await _db.Notifications.FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
            if (n != null) { n.IsRead = true; await _db.SaveChangesAsync(); }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            notifications.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId) =>
            await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task GenerateBudgetNotificationsAsync(string userId)
        {
            var today     = DateTime.Today;
            var budget    = await _db.Budgets
                .Include(b => b.BudgetCategories).ThenInclude(bc => bc.Category)
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == today.Month && b.Year == today.Year);

            if (budget == null) return;

            var monthStart   = new DateTime(today.Year, today.Month, 1);
            var totalSpent   = await _db.Transactions
                .Where(t => t.UserId == userId && t.Type == "Expense" && t.TransactionDate >= monthStart)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var pct = budget.TotalBudget > 0 ? (totalSpent / budget.TotalBudget) * 100 : 0;

            // Don't duplicate notifications — check if one already sent today
            var todayStart = DateTime.UtcNow.Date;

            if (pct >= 100)
                await AddNotificationIfNewAsync(userId, $"🚨 Monthly budget exceeded! You have spent ₹{totalSpent:N2} of ₹{budget.TotalBudget:N2}.", "Danger", todayStart);
            else if (pct >= 80)
                await AddNotificationIfNewAsync(userId, $"⚠️ You have used {pct:F1}% of your monthly budget (₹{totalSpent:N2} of ₹{budget.TotalBudget:N2}).", "Warning", todayStart);

            // Per-category alerts
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            foreach (var bc in budget.BudgetCategories)
            {
                if (bc.Amount <= 0) continue;
                var catSpent = await _db.Transactions
                    .Where(t => t.UserId == userId && t.Type == "Expense" && t.CategoryId == bc.CategoryId
                             && t.TransactionDate >= monthStart && t.TransactionDate <= monthEnd)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0;

                var catPct = (catSpent / bc.Amount) * 100;
                if (catPct >= 100)
                    await AddNotificationIfNewAsync(userId, $"🚨 {bc.Category.Name} budget exceeded! Spent ₹{catSpent:N2} of ₹{bc.Amount:N2}.", "Danger", todayStart);
                else if (catPct >= 80)
                    await AddNotificationIfNewAsync(userId, $"⚠️ {bc.Category.Name} budget at {catPct:F1}% (₹{catSpent:N2} of ₹{bc.Amount:N2}).", "Warning", todayStart);
            }
        }

        private async Task AddNotificationIfNewAsync(string userId, string message, string type, DateTime since)
        {
            var exists = await _db.Notifications.AnyAsync(n =>
                n.UserId == userId && n.Message == message && n.CreatedDate >= since);
            if (!exists)
            {
                _db.Notifications.Add(new Notification { UserId = userId, Message = message, Type = type });
                await _db.SaveChangesAsync();
            }
        }
    }

    /// <summary>
    /// Manages recurring expense rules and generates transactions when they are due.
    /// </summary>
    public class RecurringExpenseService : IRecurringExpenseService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICategoryService _categoryService;
        private readonly IPaymentMethodService _pmService;

        public RecurringExpenseService(ApplicationDbContext db, ICategoryService cat, IPaymentMethodService pm)
        {
            _db              = db;
            _categoryService = cat;
            _pmService       = pm;
        }

        public async Task<List<RecurringExpenseViewModel>> GetRecurringExpensesAsync(string userId)
        {
            return await _db.RecurringExpenses
                .AsNoTracking()
                .Include(re => re.Category)
                .Include(re => re.PaymentMethod)
                .Where(re => re.UserId == userId)
                .OrderBy(re => re.Name)
                .Select(re => new RecurringExpenseViewModel
                {
                    RecurringExpenseId = re.RecurringExpenseId,
                    Name              = re.Name,
                    Amount            = re.Amount,
                    Type              = re.Type,
                    CategoryId        = re.CategoryId,
                    CategoryName      = re.Category.Name,
                    PaymentMethodId   = re.PaymentMethodId,
                    PaymentMethodName = re.PaymentMethod != null ? re.PaymentMethod.Name : null,
                    Frequency         = re.Frequency,
                    StartDate         = re.StartDate,
                    EndDate           = re.EndDate,
                    IsActive          = re.IsActive,
                    LastGeneratedDate = re.LastGeneratedDate,
                    Notes             = re.Notes
                })
                .ToListAsync();
        }

        public async Task<RecurringExpenseViewModel?> GetByIdAsync(int id, string userId)
        {
            var re = await _db.RecurringExpenses
                .AsNoTracking()
                .Include(r => r.Category)
                .Include(r => r.PaymentMethod)
                .FirstOrDefaultAsync(r => r.RecurringExpenseId == id && r.UserId == userId);
            if (re == null) return null;

            return new RecurringExpenseViewModel
            {
                RecurringExpenseId = re.RecurringExpenseId,
                Name              = re.Name,
                Amount            = re.Amount,
                Type              = re.Type,
                CategoryId        = re.CategoryId,
                CategoryName      = re.Category.Name,
                PaymentMethodId   = re.PaymentMethodId,
                PaymentMethodName = re.PaymentMethod?.Name,
                Frequency         = re.Frequency,
                StartDate         = re.StartDate,
                EndDate           = re.EndDate,
                IsActive          = re.IsActive,
                LastGeneratedDate = re.LastGeneratedDate,
                Notes             = re.Notes,
                Categories        = await _categoryService.GetCategorySelectListAsync(userId),
                PaymentMethods    = await _pmService.GetPaymentMethodSelectListAsync(userId)
            };
        }

        public async Task<int> CreateAsync(string userId, RecurringExpenseViewModel vm)
        {
            var re = new RecurringExpense
            {
                UserId          = userId,
                Name            = vm.Name.Trim(),
                Amount          = vm.Amount,
                Type            = vm.Type,
                CategoryId      = vm.CategoryId,
                PaymentMethodId = vm.PaymentMethodId,
                Notes           = vm.Notes?.Trim(),
                Frequency       = vm.Frequency,
                StartDate       = vm.StartDate,
                EndDate         = vm.EndDate,
                IsActive        = true,
                CreatedDate     = DateTime.UtcNow
            };
            _db.RecurringExpenses.Add(re);
            await _db.SaveChangesAsync();
            return re.RecurringExpenseId;
        }

        public async Task<bool> UpdateAsync(string userId, RecurringExpenseViewModel vm)
        {
            var re = await _db.RecurringExpenses
                .FirstOrDefaultAsync(r => r.RecurringExpenseId == vm.RecurringExpenseId && r.UserId == userId);
            if (re == null) return false;
            re.Name            = vm.Name.Trim();
            re.Amount          = vm.Amount;
            re.Type            = vm.Type;
            re.CategoryId      = vm.CategoryId;
            re.PaymentMethodId = vm.PaymentMethodId;
            re.Notes           = vm.Notes?.Trim();
            re.Frequency       = vm.Frequency;
            re.StartDate       = vm.StartDate;
            re.EndDate         = vm.EndDate;
            re.IsActive        = vm.IsActive;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id, string userId)
        {
            var re = await _db.RecurringExpenses
                .FirstOrDefaultAsync(r => r.RecurringExpenseId == id && r.UserId == userId);
            if (re == null) return false;
            _db.RecurringExpenses.Remove(re);
            await _db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Called at login or on a schedule to auto-generate transactions for due recurring expenses.
        /// Uses LastGeneratedDate to avoid duplicates.
        /// </summary>
        public async Task GenerateDueTransactionsAsync(string userId)
        {
            var today = DateTime.Today;
            var recurring = await _db.RecurringExpenses
                .Where(re => re.UserId == userId && re.IsActive
                          && re.StartDate <= today
                          && (re.EndDate == null || re.EndDate >= today))
                .ToListAsync();

            foreach (var re in recurring)
            {
                var nextDate = GetNextDueDate(re);
                if (nextDate == null || nextDate.Value > today) continue;

                // Generate transaction
                _db.Transactions.Add(new Transaction
                {
                    UserId             = userId,
                    Type               = re.Type,
                    Amount             = re.Amount,
                    CategoryId         = re.CategoryId,
                    PaymentMethodId    = re.PaymentMethodId,
                    Description        = $"{re.Name} (Auto-generated)",
                    Notes              = re.Notes,
                    TransactionDate    = nextDate.Value,
                    RecurringExpenseId = re.RecurringExpenseId,
                    CreatedDate        = DateTime.UtcNow,
                    UpdatedDate        = DateTime.UtcNow
                });

                re.LastGeneratedDate = nextDate.Value;
            }

            await _db.SaveChangesAsync();
        }

        private DateTime? GetNextDueDate(RecurringExpense re)
        {
            var last = re.LastGeneratedDate ?? re.StartDate.AddDays(-1);
            return re.Frequency switch
            {
                "Daily"   => last.AddDays(1),
                "Weekly"  => last.AddDays(7),
                "Monthly" => last.AddMonths(1),
                "Yearly"  => last.AddYears(1),
                _         => null
            };
        }
    }
}
