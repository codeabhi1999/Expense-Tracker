using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.Services;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly ITransactionService  _txService;
        private readonly ICategoryService     _catService;
        private readonly IPaymentMethodService _pmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionsController(
            ITransactionService      txService,
            ICategoryService         catService,
            IPaymentMethodService    pmService,
            UserManager<ApplicationUser> userManager)
        {
            _txService   = txService;
            _catService  = catService;
            _pmService   = pmService;
            _userManager = userManager;
        }

        private string UserId => _userManager.GetUserId(User)!;

        // ── GET /Transactions ─────────────────────────────────────────
        public async Task<IActionResult> Index(TransactionFilterViewModel filter)
        {
            var result = await _txService.GetFilteredTransactionsAsync(UserId, filter);
            return View(result);
        }

        // ── GET /Transactions/Create ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create(string type = "Expense")
        {
            var vm = new TransactionViewModel
            {
                Type            = type,
                TransactionDate = DateTime.Today,
                Categories      = await _catService.GetCategorySelectListAsync(UserId, type),
                PaymentMethods  = await _pmService.GetPaymentMethodSelectListAsync(UserId)
            };
            return View(vm);
        }

        // ── POST /Transactions/Create ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories     = await _catService.GetCategorySelectListAsync(UserId, model.Type);
                model.PaymentMethods = await _pmService.GetPaymentMethodSelectListAsync(UserId);
                return View(model);
            }

            await _txService.CreateTransactionAsync(UserId, model);
            TempData["Success"] = $"{model.Type} of ₹{model.Amount:N2} added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /Transactions/Edit/5 ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _txService.GetTransactionByIdAsync(id, UserId);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // ── POST /Transactions/Edit/5 ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TransactionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories     = await _catService.GetCategorySelectListAsync(UserId, model.Type);
                model.PaymentMethods = await _pmService.GetPaymentMethodSelectListAsync(UserId);
                return View(model);
            }

            var success = await _txService.UpdateTransactionAsync(UserId, model);
            if (!success) return NotFound();

            TempData["Success"] = "Transaction updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /Transactions/Details/5 ───────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _txService.GetTransactionByIdAsync(id, UserId);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // ── POST /Transactions/Delete/5 ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _txService.DeleteTransactionAsync(id, UserId);
            if (!success)
            {
                TempData["Error"] = "Transaction not found or could not be deleted.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Success"] = "Transaction deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /Transactions/Export?format=csv ───────────────────────
        [HttpGet]
        public async Task<IActionResult> Export(TransactionFilterViewModel filter, string format = "csv")
        {
            if (format.ToLower() == "excel")
            {
                var ms = new MemoryStream();
                await _txService.ExportToExcelAsync(UserId, filter, ms);
                ms.Position = 0;
                return File(ms, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Transactions_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            else
            {
                var ms = new MemoryStream();
                await _txService.ExportToCsvAsync(UserId, filter, ms);
                ms.Position = 0;
                return File(ms, "text/csv", $"Transactions_{DateTime.Now:yyyyMMdd}.csv");
            }
        }

        // ── AJAX: GET categories by type ──────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetCategoriesByType(string type)
        {
            var categories = await _catService.GetCategorySelectListAsync(UserId, type);
            return Json(categories.Select(c => new { value = c.Value, text = c.Text }));
        }
    }
}
