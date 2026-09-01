using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.Services;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _catService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoriesController(ICategoryService catService, UserManager<ApplicationUser> um)
        {
            _catService  = catService;
            _userManager = um;
        }

        private string UserId => _userManager.GetUserId(User)!;

        public async Task<IActionResult> Index()
        {
            var categories = await _catService.GetCategoriesAsync(UserId);
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create() => View(new CategoryViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            await _catService.CreateCategoryAsync(UserId, model);
            TempData["Success"] = $"Category '{model.Name}' created!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _catService.GetCategoryByIdAsync(id, UserId);
            if (vm == null || vm.IsDefault) return NotFound();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var success = await _catService.UpdateCategoryAsync(UserId, model);
            if (!success) return NotFound();
            TempData["Success"] = "Category updated!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _catService.DeleteCategoryAsync(id, UserId);
            if (!success)
                TempData["Error"] = "Cannot delete this category. It is being used by transactions. Please reassign transactions first.";
            else
                TempData["Success"] = "Category deleted.";
            return RedirectToAction(nameof(Index));
        }
    }

    [Authorize]
    public class PaymentMethodsController : Controller
    {
        private readonly IPaymentMethodService _pmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentMethodsController(IPaymentMethodService pmService, UserManager<ApplicationUser> um)
        {
            _pmService   = pmService;
            _userManager = um;
        }

        private string UserId => _userManager.GetUserId(User)!;

        public async Task<IActionResult> Index()
        {
            var methods = await _pmService.GetPaymentMethodsAsync(UserId);
            return View(methods);
        }

        [HttpGet]
        public IActionResult Create() => View(new PaymentMethodViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentMethodViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            await _pmService.CreatePaymentMethodAsync(UserId, model);
            TempData["Success"] = $"Payment method '{model.Name}' added!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _pmService.GetPaymentMethodByIdAsync(id, UserId);
            if (vm == null || vm.IsDefault) return NotFound();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PaymentMethodViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var success = await _pmService.UpdatePaymentMethodAsync(UserId, model);
            if (!success) return NotFound();
            TempData["Success"] = "Payment method updated!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _pmService.DeletePaymentMethodAsync(id, UserId);
            if (!success)
                TempData["Error"] = "Cannot delete this payment method. It is being used by transactions.";
            else
                TempData["Success"] = "Payment method deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
