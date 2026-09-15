using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.Services;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Controllers
{
    [Authorize]
    public class AiInsightsController : Controller
    {
        private readonly IAiService _aiService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AiInsightsController(IAiService aiService, UserManager<ApplicationUser> userManager)
        {
            _aiService = aiService;
            _userManager = userManager;
        }

        // ── GET /AiInsights ──────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var vm = await _aiService.GetAiInsightsDashboardAsync(userId);
            return View(vm);
        }

        // ── POST /AiInsights/Ask (AI Copilot Q&A) ─────────────────────
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AiChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required." });
            }

            var userId = _userManager.GetUserId(User)!;
            var response = await _aiService.AskFinancialCopilotAsync(userId, request.Message);
            return Json(response);
        }

        // ── POST /AiInsights/ParseQuickAdd (Natural Language Parser) ──
        [HttpPost]
        public async Task<IActionResult> ParseQuickAdd([FromBody] QuickAddRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new { success = false, message = "Please provide transaction text." });
            }

            var userId = _userManager.GetUserId(User)!;
            var parsed = await _aiService.ParseNaturalLanguageTransactionAsync(userId, request.Text);
            return Json(parsed);
        }

        // ── POST /AiInsights/ConfirmQuickAdd (1-Click Save) ───────────
        [HttpPost]
        public async Task<IActionResult> ConfirmQuickAdd([FromBody] AiParsedTransaction parsed)
        {
            if (parsed == null || parsed.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid transaction payload." });
            }

            var userId = _userManager.GetUserId(User)!;
            var success = await _aiService.SaveParsedTransactionAsync(userId, parsed);

            if (success)
            {
                return Json(new { success = true, message = $"Successfully logged {parsed.Type} of ₹{parsed.Amount:N2} for '{parsed.Description}'!" });
            }

            return StatusCode(500, new { success = false, message = "Failed to save transaction." });
        }
    }

    public class QuickAddRequest
    {
        public string Text { get; set; } = string.Empty;
    }
}
