using Microsoft.EntityFrameworkCore;
using MyDailyExpenseTracker.Data;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Services
{
    /// <summary>
    /// Handles all dashboard summary calculations using LINQ.
    /// All data is fetched from the database filtered by userId.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;

        public DashboardService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(string userId)
        {
            var today     = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var prevStart  = monthStart.AddMonths(-1);
            var prevEnd    = monthStart.AddDays(-1);

            // ── Base query — only this user's transactions ─────────────
            var baseQuery = _db.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId);

            // ── Summary card calculations ──────────────────────────────
            var todayData = await baseQuery
                .Where(t => t.TransactionDate.Date == today)
                .GroupBy(t => t.Type)
                .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
                .ToListAsync();

            var monthData = await baseQuery
                .Where(t => t.TransactionDate >= monthStart && t.TransactionDate <= today)
                .GroupBy(t => t.Type)
                .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
                .ToListAsync();

            var allTimeIncome  = await baseQuery.Where(t => t.Type == "Income").SumAsync(t => (decimal?)t.Amount) ?? 0;
            var allTimeExpense = await baseQuery.Where(t => t.Type == "Expense").SumAsync(t => (decimal?)t.Amount) ?? 0;

            var totalTxCount = await baseQuery.CountAsync();

            // ── Previous month expense (for comparison) ────────────────
            var prevMonthExpense = await baseQuery
                .Where(t => t.Type == "Expense" && t.TransactionDate >= prevStart && t.TransactionDate <= prevEnd)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            // ── Recent 10 transactions ─────────────────────────────────
            var recent = await baseQuery
                .Include(t => t.Category)
                .Include(t => t.PaymentMethod)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedDate)
                .Take(10)
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
                    PaymentMethodName = t.PaymentMethod != null ? t.PaymentMethod.Name : null
                })
                .ToListAsync();

            // ── Monthly Expense Chart (last 6 months) ──────────────────
            var sixMonthsAgo = monthStart.AddMonths(-5);
            var monthlyExpenseRaw = await baseQuery
                .Where(t => t.Type == "Expense" && t.TransactionDate >= sixMonthsAgo)
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .Select(g => new
                {
                    Year   = g.Key.Year,
                    Month  = g.Key.Month,
                    Total  = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            var monthlyExpenseChart = new Dictionary<string, decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var m   = monthStart.AddMonths(-i);
                var key = m.ToString("MMM yy");
                var val = monthlyExpenseRaw.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Total ?? 0;
                monthlyExpenseChart[key] = val;
            }

            // ── Income vs Expense Chart (last 6 months) ────────────────
            var monthlyAllRaw = await baseQuery
                .Where(t => t.TransactionDate >= sixMonthsAgo)
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.Type })
                .Select(g => new
                {
                    Year   = g.Key.Year,
                    Month  = g.Key.Month,
                    Type   = g.Key.Type,
                    Total  = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            var incomeVsExpenseChart = new Dictionary<string, MonthlyIncomeExpense>();
            for (int i = 5; i >= 0; i--)
            {
                var m   = monthStart.AddMonths(-i);
                var key = m.ToString("MMM yy");
                incomeVsExpenseChart[key] = new MonthlyIncomeExpense
                {
                    Income  = monthlyAllRaw.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month && x.Type == "Income")?.Total ?? 0,
                    Expense = monthlyAllRaw.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month && x.Type == "Expense")?.Total ?? 0
                };
            }

            // ── Category Expense Chart (current month) ─────────────────
            var categoryChart = await baseQuery
                .Where(t => t.Type == "Expense" && t.TransactionDate >= monthStart && t.TransactionDate <= today)
                .Include(t => t.Category)
                .GroupBy(t => t.Category.Name)
                .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
                .OrderByDescending(g => g.Total)
                .Take(8)
                .ToDictionaryAsync(g => g.Category, g => g.Total);

            // ── Daily Expense Trend (current month) ────────────────────
            var dailyRaw = await baseQuery
                .Where(t => t.Type == "Expense" && t.TransactionDate >= monthStart && t.TransactionDate <= today)
                .GroupBy(t => t.TransactionDate.Day)
                .Select(g => new { Day = g.Key, Total = g.Sum(t => t.Amount) })
                .ToListAsync();

            var dailyChart = new Dictionary<string, decimal>();
            for (int d = 1; d <= today.Day; d++)
            {
                dailyChart[d.ToString()] = dailyRaw.FirstOrDefault(x => x.Day == d)?.Total ?? 0;
            }

            // ── Payment Method Chart (current month) ───────────────────
            var paymentChart = await baseQuery
                .Where(t => t.Type == "Expense" && t.TransactionDate >= monthStart && t.TransactionDate <= today && t.PaymentMethodId != null)
                .Include(t => t.PaymentMethod)
                .GroupBy(t => t.PaymentMethod!.Name)
                .Select(g => new { Method = g.Key, Total = g.Sum(t => t.Amount) })
                .ToDictionaryAsync(g => g.Method, g => g.Total);

            // ── Budget ─────────────────────────────────────────────────
            var budget = await _db.Budgets
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == today.Month && b.Year == today.Year);

            // ── Unread Notifications ───────────────────────────────────
            var unreadCount = await _db.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return new DashboardViewModel
            {
                TodayExpense                 = todayData.FirstOrDefault(d => d.Type == "Expense")?.Total ?? 0,
                TodayIncome                  = todayData.FirstOrDefault(d => d.Type == "Income")?.Total ?? 0,
                MonthExpense                 = monthData.FirstOrDefault(d => d.Type == "Expense")?.Total ?? 0,
                MonthIncome                  = monthData.FirstOrDefault(d => d.Type == "Income")?.Total ?? 0,
                TotalBalance                 = allTimeIncome - allTimeExpense,
                TotalTransactions            = totalTxCount,
                PrevMonthExpense             = prevMonthExpense,
                RecentTransactions           = recent,
                MonthlyExpenseChart          = monthlyExpenseChart,
                MonthlyIncomeVsExpenseChart  = incomeVsExpenseChart,
                CategoryExpenseChart         = categoryChart,
                DailyExpenseTrend            = dailyChart,
                PaymentMethodChart           = paymentChart,
                MonthlyBudget                = budget?.TotalBudget,
                UnreadNotificationCount      = unreadCount
            };
        }
    }
}
