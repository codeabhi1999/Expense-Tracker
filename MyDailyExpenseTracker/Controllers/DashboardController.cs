using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.Services;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService        _dashboardService;
        private readonly INotificationService     _notificationService;
        private readonly IRecurringExpenseService _recurringService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            IDashboardService        dashboardService,
            INotificationService     notificationService,
            IRecurringExpenseService recurringService,
            UserManager<ApplicationUser> userManager)
        {
            _dashboardService    = dashboardService;
            _notificationService = notificationService;
            _recurringService    = recurringService;
            _userManager         = userManager;
        }

        // ── GET / (default route → Dashboard/Index) ────────────────
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            // Auto-generate any due recurring transactions on every dashboard load
            await _recurringService.GenerateDueTransactionsAsync(userId);

            // Auto-generate budget notifications
            await _notificationService.GenerateBudgetNotificationsAsync(userId);

            var vm = await _dashboardService.GetDashboardDataAsync(userId);
            return View(vm);
        }
    }
}
