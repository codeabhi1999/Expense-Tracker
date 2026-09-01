using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.Services;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Controllers
{
    [Authorize]
    public class BudgetController : Controller
    {
        private readonly IBudgetService  _budgetService;
        private readonly UserManager<ApplicationUser> _userManager;

        public BudgetController(IBudgetService budgetService, UserManager<ApplicationUser> um)
        {
            _budgetService = budgetService;
            _userManager   = um;
        }

        private string UserId => _userManager.GetUserId(User)!;

        // ── GET /Budget ───────────────────────────────────────────────
        public async Task<IActionResult> Index(int month = 0, int year = 0)
        {
            if (month == 0) month = DateTime.Now.Month;
            if (year  == 0) year  = DateTime.Now.Year;

            var vm = await _budgetService.GetOrCreateBudgetAsync(UserId, month, year);
            return View(vm);
        }

        // ── POST /Budget/Save ─────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(BudgetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var refreshed = await _budgetService.GetOrCreateBudgetAsync(UserId, model.Month, model.Year);
                refreshed.TotalBudget     = model.TotalBudget;
                refreshed.Notes           = model.Notes;
                return View("Index", refreshed);
            }

            await _budgetService.SaveBudgetAsync(UserId, model);
            TempData["Success"] = $"Budget for {new DateTime(model.Year, model.Month, 1):MMMM yyyy} saved successfully!";
            return RedirectToAction(nameof(Index), new { month = model.Month, year = model.Year });
        }
    }

    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(IReportService reportService, UserManager<ApplicationUser> um)
        {
            _reportService = reportService;
            _userManager   = um;
        }

        private string UserId => _userManager.GetUserId(User)!;

        // ── GET /Reports ──────────────────────────────────────────────
        public async Task<IActionResult> Index(int month = 0, int year = 0)
        {
            if (month == 0) month = DateTime.Now.Month;
            if (year  == 0) year  = DateTime.Now.Year;

            var vm = await _reportService.GetMonthlyReportAsync(UserId, month, year);
            return View(vm);
        }
    }

    [Authorize]
    public class RecurringExpensesController : Controller
    {
        private readonly IRecurringExpenseService _recurringService;
        private readonly ICategoryService         _catService;
        private readonly IPaymentMethodService    _pmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public RecurringExpensesController(
            IRecurringExpenseService recurringService,
            ICategoryService catService,
            IPaymentMethodService pmService,
            UserManager<ApplicationUser> um)
        {
            _recurringService = recurringService;
            _catService       = catService;
            _pmService        = pmService;
            _userManager      = um;
        }

        private string UserId => _userManager.GetUserId(User)!;

        public async Task<IActionResult> Index()
        {
            var list = await _recurringService.GetRecurringExpensesAsync(UserId);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new RecurringExpenseViewModel
            {
                StartDate      = DateTime.Today,
                Categories     = await _catService.GetCategorySelectListAsync(UserId),
                PaymentMethods = await _pmService.GetPaymentMethodSelectListAsync(UserId)
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecurringExpenseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories     = await _catService.GetCategorySelectListAsync(UserId);
                model.PaymentMethods = await _pmService.GetPaymentMethodSelectListAsync(UserId);
                return View(model);
            }
            await _recurringService.CreateAsync(UserId, model);
            TempData["Success"] = $"Recurring expense '{model.Name}' created!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _recurringService.GetByIdAsync(id, UserId);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RecurringExpenseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories     = await _catService.GetCategorySelectListAsync(UserId);
                model.PaymentMethods = await _pmService.GetPaymentMethodSelectListAsync(UserId);
                return View(model);
            }
            var success = await _recurringService.UpdateAsync(UserId, model);
            if (!success) return NotFound();
            TempData["Success"] = "Recurring expense updated!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _recurringService.DeleteAsync(id, UserId);
            TempData["Success"] = "Recurring expense deleted.";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(INotificationService ns, UserManager<ApplicationUser> um)
        {
            _notificationService = ns;
            _userManager         = um;
        }

        private string UserId => _userManager.GetUserId(User)!;

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id, UserId);
            return Ok();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            await _notificationService.MarkAllAsReadAsync(UserId);
            TempData["Success"] = "All notifications marked as read.";
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> GetUnread()
        {
            var notifications = await _notificationService.GetUnreadNotificationsAsync(UserId);
            return Json(notifications.Select(n => new
            {
                n.NotificationId,
                n.Message,
                n.Type,
                CreatedDate = n.CreatedDate.ToString("dd MMM hh:mm tt")
            }));
        }
    }
}
