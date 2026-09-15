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
        private readonly IAiService               _aiService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            IDashboardService        dashboardService,
            INotificationService     notificationService,
            IRecurringExpenseService recurringService,
            IAiService               aiService,
            UserManager<ApplicationUser> userManager)
        {
            _dashboardService    = dashboardService;
            _notificationService = notificationService;
            _recurringService    = recurringService;
            _aiService           = aiService;
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

            // Enrich with AI Insights
            var aiData = await _aiService.GetAiInsightsDashboardAsync(userId);
            vm.HealthScore = aiData.HealthScore;
            vm.Forecast = aiData.Forecast;
            vm.Anomalies = aiData.Anomalies;

            return View(vm);
        }
    }
}
