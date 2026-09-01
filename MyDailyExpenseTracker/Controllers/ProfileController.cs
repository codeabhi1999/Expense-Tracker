using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyDailyExpenseTracker.Data;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser>  _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public ProfileController(
            UserManager<ApplicationUser>  userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext db)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _db            = db;
        }

        // ── GET /Profile ──────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var txCount = _db.Transactions.Count(t => t.UserId == user.Id);

            return View(new ProfileViewModel
            {
                FirstName        = user.FirstName,
                LastName         = user.LastName,
                Email            = user.Email!,
                PhoneNumber      = user.PhoneNumber,
                Currency         = user.Currency,
                DateFormat       = user.DateFormat,
                Theme            = user.Theme,
                MemberSince      = user.CreatedDate,
                TotalTransactions = txCount
            });
        }

        // ── POST /Profile ─────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.FirstName   = model.FirstName.Trim();
            user.LastName    = model.LastName.Trim();
            user.PhoneNumber = model.PhoneNumber;
            user.Currency    = model.Currency;
            user.DateFormat  = model.DateFormat;
            user.Theme       = model.Theme;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            // Refresh the sign-in cookie (in case email changed)
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ── GET /Profile/ChangePassword ───────────────────────────────
        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        // ── POST /Profile/ChangePassword ──────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ── POST /Profile/SetTheme ────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetTheme(string theme)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.Theme = theme == "dark" ? "dark" : "light";
                await _userManager.UpdateAsync(user);
            }
            return Ok();
        }
    }
}
