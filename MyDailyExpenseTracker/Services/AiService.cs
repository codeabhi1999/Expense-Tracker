using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyDailyExpenseTracker.Data;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Services
{
    public class AiService : IAiService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiService> _logger;

        public AiService(ApplicationDbContext db, IConfiguration configuration, ILogger<AiService> logger)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AiInsightsViewModel> GetAiInsightsDashboardAsync(string userId)
        {
            var vm = new AiInsightsViewModel
            {
                CurrencySymbol = _configuration["AppSettings:DefaultCurrency"] == "INR" ? "₹" : "$"
            };

            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var prevMonthStart = monthStart.AddMonths(-1);
            var prevMonthEnd = monthStart.AddDays(-1);

            // User transactions for current & previous month
            var currentMonthTx = await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.PaymentMethod)
                .Where(t => t.UserId == userId && t.TransactionDate >= monthStart && t.TransactionDate <= today)
                .ToListAsync();

            var prevMonthTx = await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.TransactionDate >= prevMonthStart && t.TransactionDate <= prevMonthEnd)
                .ToListAsync();

            var currentBudget = await _db.Budgets
                .AsNoTracking()
                .Include(b => b.BudgetCategories)
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == today.Month && b.Year == today.Year);

            // ── 1. Calculate Burn Rate & Forecast ─────────────────────
            vm.Forecast = CalculateBurnRateForecast(currentMonthTx, currentBudget?.TotalBudget, today);

            // ── 2. Calculate Financial Health Score ────────────────────
            vm.HealthScore = CalculateFinancialHealthScore(currentMonthTx, prevMonthTx, currentBudget?.TotalBudget, vm.Forecast);

            // ── 3. Detect Anomalies ───────────────────────────────────
            vm.Anomalies = DetectAnomalies(currentMonthTx, prevMonthTx, vm.Forecast);

            // ── 4. Smart Suggestions ──────────────────────────────────
            vm.Suggestions = GenerateSmartSuggestions(currentMonthTx, vm.HealthScore, vm.Forecast, vm.Anomalies);

            // ── 5. Category Trends ────────────────────────────────────
            vm.CategoryTrends = GenerateCategoryTrends(currentMonthTx, prevMonthTx);

            return vm;
        }

        public async Task<AiChatResponse> AskFinancialCopilotAsync(string userId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new AiChatResponse
                {
                    Answer = "Hello! I am your AI Financial Copilot. You can ask me anything about your spending, budget health, or ways to save money.",
                    FollowUpSuggestions = new List<string>
                    {
                        "Where did I spend the most this month?",
                        "How can I save ₹3,000 more this month?",
                        "Will I exceed my monthly budget?",
                        "How is my financial health score?"
                    }
                };
            }

            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var currentMonthTx = await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.PaymentMethod)
                .Where(t => t.UserId == userId && t.TransactionDate >= monthStart && t.TransactionDate <= today)
                .ToListAsync();

            var currentBudget = await _db.Budgets
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.UserId == userId && b.Month == today.Month && b.Year == today.Year);

            var forecast = CalculateBurnRateForecast(currentMonthTx, currentBudget?.TotalBudget, today);
            var lowerQuery = query.ToLowerInvariant().Trim();

            // ── Intent Matching & Response Generation ───────────────
            string answer;
            var followUps = new List<string>();

            if (lowerQuery.Contains("most") || lowerQuery.Contains("highest") || lowerQuery.Contains("top expense") || lowerQuery.Contains("biggest"))
            {
                var topCategory = currentMonthTx
                    .Where(t => t.Type == "Expense")
                    .GroupBy(t => t.Category.Name)
                    .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
                    .OrderByDescending(x => x.Total)
                    .FirstOrDefault();

                var largestTx = currentMonthTx
                    .Where(t => t.Type == "Expense")
                    .OrderByDescending(t => t.Amount)
                    .FirstOrDefault();

                if (topCategory != null)
                {
                    answer = $"📊 **Top Spending Category:**\n\nYou've spent the most on **{topCategory.Category}** this month, totaling **₹{topCategory.Total:N2}**.\n\n" +
                             (largestTx != null ? $"Your largest single expense was **₹{largestTx.Amount:N2}** for *\"{largestTx.Description}\"* on {largestTx.TransactionDate:dd MMM}." : "");
                }
                else
                {
                    answer = "You haven't recorded any expenses for this month yet. Log your first expense using the quick-add bar above!";
                }

                followUps.Add("How can I reduce spending in this category?");
                followUps.Add("Show my daily burn rate");
                followUps.Add("How is my financial health score?");
            }
            else if (lowerQuery.Contains("save") || lowerQuery.Contains("cut") || lowerQuery.Contains("optimize") || lowerQuery.Contains("advice") || lowerQuery.Contains("tip"))
            {
                var totalExpenses = currentMonthTx.Where(t => t.Type == "Expense").Sum(t => t.Amount);
                var potentialDiscretionary = currentMonthTx
                    .Where(t => t.Type == "Expense" && IsDiscretionary(t.Category?.Name ?? ""))
                    .GroupBy(t => t.Category.Name)
                    .Select(g => new { Name = g.Key, Amount = g.Sum(x => x.Amount) })
                    .OrderByDescending(x => x.Amount)
                    .FirstOrDefault();

                decimal targetSave = Math.Round(totalExpenses * 0.15m, 0);
                if (targetSave <= 0) targetSave = 2000;

                answer = $"💡 **AI Savings Strategy:**\n\n" +
                         $"1. **Target Opportunity**: If you reduce discretionary spending by just 15%, you can save approximately **₹{targetSave:N2}** by month-end.\n" +
                         (potentialDiscretionary != null ? $"2. **Primary Focus**: You have spent **₹{potentialDiscretionary.Amount:N2}** on **{potentialDiscretionary.Name}**. Trimming 2-3 non-essential orders here could immediately unlock ₹{Math.Round(potentialDiscretionary.Amount * 0.20m, 0):N2} in savings.\n" : "") +
                         $"3. **Safe Daily Cap**: Limit your daily variable spend to **₹{forecast.SafeDailyBudget:N2}/day** for the remaining {forecast.DaysRemaining} days to stay comfortably ahead.";

                followUps.Add("Where did I spend the most this month?");
                followUps.Add("Will I exceed my monthly budget?");
            }
            else if (lowerQuery.Contains("budget") || lowerQuery.Contains("exceed") || lowerQuery.Contains("burn") || lowerQuery.Contains("runway") || lowerQuery.Contains("pace"))
            {
                if (forecast.MonthlyBudget.HasValue && forecast.MonthlyBudget.Value > 0)
                {
                    if (forecast.WillExceedBudget)
                    {
                        answer = $"⚠️ **Budget Pace Alert:**\n\n" +
                                 $"At your current daily burn rate of **₹{forecast.DailyBurnRate:N2}/day**, you are projected to reach **₹{forecast.ProjectedMonthEndExpense:N2}** by month-end.\n\n" +
                                 $"• **Monthly Budget:** ₹{forecast.MonthlyBudget:N2}\n" +
                                 $"• **Projected Overrun:** **₹{Math.Abs(forecast.ProjectedBalance):N2}**\n" +
                                 $"• **Safe Recommended Pace:** To avoid exceeding your budget, cap spending at **₹{forecast.SafeDailyBudget:N2}/day** for the next {forecast.DaysRemaining} days.";
                    }
                    else
                    {
                        answer = $"✅ **Budget On Track!**\n\n" +
                                 $"Your spending velocity is well controlled. At your current pace of **₹{forecast.DailyBurnRate:N2}/day**, you are projected to end the month at **₹{forecast.ProjectedMonthEndExpense:N2}**.\n\n" +
                                 $"• **Monthly Budget:** ₹{forecast.MonthlyBudget:N2}\n" +
                                 $"• **Estimated Surplus:** **₹{forecast.ProjectedBalance:N2}** remaining.\n" +
                                 $"• **Safe Daily Allowance:** Up to **₹{forecast.SafeDailyBudget:N2}/day**.";
                    }
                }
                else
                {
                    answer = $"📈 **Burn Rate Overview:**\n\n" +
                             $"You are spending an average of **₹{forecast.DailyBurnRate:N2}/day** this month, with total month-to-date expenses at **₹{forecast.MonthToDateExpense:N2}**.\n\n" +
                             $"*Tip: Set a Monthly Budget in the Budget module so I can calculate your exact runway and alert you before you overspend.*";
                }

                followUps.Add("How can I save ₹3,000 more this month?");
                followUps.Add("Show my weekend vs weekday spending");
            }
            else if (lowerQuery.Contains("health") || lowerQuery.Contains("score") || lowerQuery.Contains("grade") || lowerQuery.Contains("rating"))
            {
                var prevMonthTx = await _db.Transactions
                    .AsNoTracking()
                    .Include(t => t.Category)
                    .Where(t => t.UserId == userId && t.TransactionDate >= monthStart.AddMonths(-1) && t.TransactionDate < monthStart)
                    .ToListAsync();

                var health = CalculateFinancialHealthScore(currentMonthTx, prevMonthTx, currentBudget?.TotalBudget, forecast);

                answer = $"🎯 **Financial Health Diagnostics:**\n\n" +
                         $"• **Overall Score:** **{health.Score} / 100** (Grade: **{health.Grade}** — *{health.StatusBadge}*)\n" +
                         $"• **Savings Ratio:** {health.SavingsRatioScore}/100\n" +
                         $"• **Budget Discipline:** {health.BudgetDisciplineScore}/100\n" +
                         $"• **Spending Stability:** {health.SpendingStabilityScore}/100\n\n" +
                         $"**Key Recommendation:** {health.Summary}";

                followUps.Add("How do I improve my health score?");
                followUps.Add("Where did I spend the most this month?");
            }
            else if (lowerQuery.Contains("weekend") || lowerQuery.Contains("weekday") || lowerQuery.Contains("saturday") || lowerQuery.Contains("sunday"))
            {
                var weekendExpenses = currentMonthTx
                    .Where(t => t.Type == "Expense" && (t.TransactionDate.DayOfWeek == DayOfWeek.Saturday || t.TransactionDate.DayOfWeek == DayOfWeek.Sunday))
                    .Sum(t => t.Amount);

                var weekdayExpenses = currentMonthTx
                    .Where(t => t.Type == "Expense" && t.TransactionDate.DayOfWeek != DayOfWeek.Saturday && t.TransactionDate.DayOfWeek != DayOfWeek.Sunday)
                    .Sum(t => t.Amount);

                answer = $"🗓️ **Weekend vs Weekday Spending Analysis:**\n\n" +
                         $"• **Weekend Total:** ₹{weekendExpenses:N2}\n" +
                         $"• **Weekday Total:** ₹{weekdayExpenses:N2}\n\n" +
                         (weekendExpenses > weekdayExpenses * 0.45m
                            ? "⚠️ Weekend spending is significantly elevated relative to weekdays. Leisure, dining, or shopping on Saturdays and Sundays are your primary cost drivers."
                            : "✅ Your weekend expenses are well-balanced with routine weekday living.");

                followUps.Add("How can I save ₹3,000 more this month?");
                followUps.Add("Will I exceed my monthly budget?");
            }
            else
            {
                // General smart summary
                var totalInc = currentMonthTx.Where(t => t.Type == "Income").Sum(t => t.Amount);
                var totalExp = currentMonthTx.Where(t => t.Type == "Expense").Sum(t => t.Amount);
                var net = totalInc - totalExp;

                answer = $"✨ **Financial Summary for {today:MMMM yyyy}:**\n\n" +
                         $"• **Income:** ₹{totalInc:N2}\n" +
                         $"• **Expense:** ₹{totalExp:N2}\n" +
                         $"• **Net Position:** **{(net >= 0 ? "+" : "")}₹{net:N2}**\n" +
                         $"• **Daily Velocity:** ₹{forecast.DailyBurnRate:N2}/day\n\n" +
                         $"How can I assist your financial planning today? Choose a suggestion below or ask any specific question.";

                followUps.Add("Where did I spend the most this month?");
                followUps.Add("How can I save ₹3,000 more this month?");
                followUps.Add("Will I exceed my monthly budget?");
                followUps.Add("How is my financial health score?");
            }

            return new AiChatResponse
            {
                Answer = answer,
                FollowUpSuggestions = followUps
            };
        }

        public async Task<AiParsedTransaction> ParseNaturalLanguageTransactionAsync(string userId, string input)
        {
            var result = new AiParsedTransaction
            {
                RawInput = input,
                TransactionDate = DateTime.Today
            };

            if (string.IsNullOrWhiteSpace(input))
            {
                result.Success = false;
                result.ErrorMessage = "Please provide transaction details (e.g. 'Coffee ₹150 via UPI').";
                return result;
            }

            var text = input.Trim();

            // ── 1. Amount Extraction ─────────────────────────────────
            // Matches: ₹500, Rs. 500, 500.50, INR 1200, or standalone numbers like 450
            var amountRegex = new Regex(@"(?:₹|rs\.?|inr)?\s*([0-9]+(?:,[0-9]{3})*(?:\.[0-9]{1,2})?)\b", RegexOptions.IgnoreCase);
            var matches = amountRegex.Matches(text);
            decimal extractedAmount = 0;
            string matchedAmountToken = string.Empty;

            foreach (Match m in matches)
            {
                var numStr = m.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) && val > 0)
                {
                    // Prefer numbers that are likely amounts (usually >= 1)
                    extractedAmount = val;
                    matchedAmountToken = m.Value;
                    break;
                }
            }

            if (extractedAmount <= 0)
            {
                result.Success = false;
                result.ErrorMessage = "Could not identify a valid amount. Please specify an amount (e.g. '450' or '₹1,200').";
                return result;
            }

            result.Amount = extractedAmount;

            // ── 2. Type Detection (Expense vs Income) ──────────────────
            var lowerText = text.ToLowerInvariant();
            var incomeKeywords = new[] { "salary", "freelance", "credited", "bonus", "dividend", "income", "received", "refund", "deposit", "earned", "gain" };
            if (incomeKeywords.Any(k => lowerText.Contains(k)))
            {
                result.Type = "Income";
            }
            else
            {
                result.Type = "Expense";
            }

            // ── 3. Date Detection ─────────────────────────────────────
            if (lowerText.Contains("yesterday"))
            {
                result.TransactionDate = DateTime.Today.AddDays(-1);
            }
            else if (lowerText.Contains("day before yesterday"))
            {
                result.TransactionDate = DateTime.Today.AddDays(-2);
            }

            // ── 4. Category Matching ──────────────────────────────────
            var availableCategories = await _db.Categories
                .AsNoTracking()
                .Where(c => c.UserId == userId || c.UserId == null)
                .ToListAsync();

            var categoryKeywords = new Dictionary<string, string[]>
            {
                { "Food & Dining", new[] { "coffee", "starbucks", "pizza", "domino", "burger", "mcdonald", "swiggy", "zomato", "restaurant", "dinner", "lunch", "breakfast", "cafe", "tea", "snack", "biryani", "food", "eat", "dining", "meal", "chai" } },
                { "Groceries", new[] { "dmart", "blinkit", "zepto", "instamart", "supermarket", "groceries", "grocery", "milk", "vegetables", "fruits", "ration", "kirana", "provisions" } },
                { "Transportation", new[] { "uber", "ola", "petrol", "diesel", "fuel", "metro", "bus", "train", "cab", "auto", "flight", "toll", "rapido", "parking", "ticket" } },
                { "Shopping", new[] { "amazon", "flipkart", "myntra", "clothes", "shoes", "mall", "electronics", "gadget", "shirt", "pant", "shopping", "store", "purchase" } },
                { "Bills & Utilities", new[] { "electricity", "water", "wifi", "broadband", "mobile", "recharge", "rent", "maintenance", "gas", "bill", "cylinder", "airtel", "jio" } },
                { "Entertainment", new[] { "netflix", "prime", "movie", "cinema", "game", "spotify", "concert", "hotstar", "youtube", "entertainment", "club", "outing" } },
                { "Healthcare", new[] { "doctor", "medicine", "pharmacy", "hospital", "gym", "clinic", "dental", "tablets", "health", "test", "apollo", "meds" } },
                { "Salary", new[] { "salary", "paycheck", "wages", "employer", "stipend" } },
                { "Investment", new[] { "stocks", "mutual fund", "sip", "crypto", "dividend", "investment", "gold", "zerodha", "groww" } }
            };

            Category? matchedCategory = null;

            // Check dictionary keywords first
            foreach (var kvp in categoryKeywords)
            {
                if (kvp.Value.Any(k => lowerText.Contains(k)))
                {
                    matchedCategory = availableCategories.FirstOrDefault(c =>
                        c.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                        c.Name.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Contains(c.Name, StringComparison.OrdinalIgnoreCase));
                    if (matchedCategory != null) break;
                }
            }

            // Fallback: direct match on category names
            if (matchedCategory == null)
            {
                matchedCategory = availableCategories.FirstOrDefault(c => lowerText.Contains(c.Name.ToLowerInvariant()));
            }

            // Default fallback
            if (matchedCategory == null)
            {
                if (result.Type == "Income")
                {
                    matchedCategory = availableCategories.FirstOrDefault(c => c.Type == "Income")
                                     ?? availableCategories.FirstOrDefault();
                }
                else
                {
                    matchedCategory = availableCategories.FirstOrDefault(c => c.Name.Contains("Other") || c.Name.Contains("General") || c.Name.Contains("Food"))
                                     ?? availableCategories.FirstOrDefault(c => c.Type == "Expense")
                                     ?? availableCategories.FirstOrDefault();
                }
            }

            if (matchedCategory != null)
            {
                result.CategoryId = matchedCategory.CategoryId;
                result.CategoryName = matchedCategory.Name;
                result.CategoryIcon = matchedCategory.Icon ?? "bi-tag";
            }

            // ── 5. Payment Method Matching ────────────────────────────
            var availablePaymentMethods = await _db.PaymentMethods
                .AsNoTracking()
                .Where(pm => pm.UserId == userId || pm.UserId == null)
                .ToListAsync();

            PaymentMethod? matchedMethod = null;

            if (lowerText.Contains("upi") || lowerText.Contains("gpay") || lowerText.Contains("google pay") || lowerText.Contains("phonepe") || lowerText.Contains("paytm"))
            {
                matchedMethod = availablePaymentMethods.FirstOrDefault(pm => pm.Name.Contains("UPI", StringComparison.OrdinalIgnoreCase));
            }
            else if (lowerText.Contains("cash"))
            {
                matchedMethod = availablePaymentMethods.FirstOrDefault(pm => pm.Name.Contains("Cash", StringComparison.OrdinalIgnoreCase));
            }
            else if (lowerText.Contains("credit card") || lowerText.Contains("credit"))
            {
                matchedMethod = availablePaymentMethods.FirstOrDefault(pm => pm.Name.Contains("Credit Card", StringComparison.OrdinalIgnoreCase) || pm.Name.Contains("Card", StringComparison.OrdinalIgnoreCase));
            }
            else if (lowerText.Contains("debit card") || lowerText.Contains("debit"))
            {
                matchedMethod = availablePaymentMethods.FirstOrDefault(pm => pm.Name.Contains("Debit Card", StringComparison.OrdinalIgnoreCase) || pm.Name.Contains("Card", StringComparison.OrdinalIgnoreCase));
            }
            else if (lowerText.Contains("card"))
            {
                matchedMethod = availablePaymentMethods.FirstOrDefault(pm => pm.Name.Contains("Card", StringComparison.OrdinalIgnoreCase));
            }
            else if (lowerText.Contains("net banking") || lowerText.Contains("bank transfer") || lowerText.Contains("neft") || lowerText.Contains("imps"))
            {
                matchedMethod = availablePaymentMethods.FirstOrDefault(pm => pm.Name.Contains("Net Banking", StringComparison.OrdinalIgnoreCase) || pm.Name.Contains("Bank", StringComparison.OrdinalIgnoreCase));
            }

            // Default payment method
            matchedMethod ??= availablePaymentMethods.FirstOrDefault(pm => pm.Name.Contains("UPI", StringComparison.OrdinalIgnoreCase))
                             ?? availablePaymentMethods.FirstOrDefault();

            if (matchedMethod != null)
            {
                result.PaymentMethodId = matchedMethod.PaymentMethodId;
                result.PaymentMethodName = matchedMethod.Name;
            }

            // ── 6. Clean Description ──────────────────────────────────
            var cleanDesc = text;
            if (!string.IsNullOrEmpty(matchedAmountToken))
            {
                cleanDesc = cleanDesc.Replace(matchedAmountToken, "");
            }

            // Remove common prepositions and keywords
            var removeWords = new[] { "for", "via", "with", "using", "through", "paid", "bought", "spent", "on", "at", "yesterday", "today", "rupees", "rs", "inr" };
            foreach (var w in removeWords)
            {
                cleanDesc = Regex.Replace(cleanDesc, $@"\b{w}\b", "", RegexOptions.IgnoreCase);
            }

            cleanDesc = Regex.Replace(cleanDesc, @"[^\w\s\-\&]", " ");
            cleanDesc = Regex.Replace(cleanDesc, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(cleanDesc))
            {
                cleanDesc = result.CategoryName ?? (result.Type == "Income" ? "Income received" : "General expense");
            }
            else
            {
                // Capitalize first letter
                cleanDesc = char.ToUpper(cleanDesc[0]) + (cleanDesc.Length > 1 ? cleanDesc.Substring(1) : "");
            }

            result.Description = cleanDesc;
            result.Success = true;
            result.Confidence = 0.96;

            return result;
        }

        public async Task<bool> SaveParsedTransactionAsync(string userId, AiParsedTransaction parsed)
        {
            if (parsed == null || parsed.Amount <= 0 || !parsed.CategoryId.HasValue)
                return false;

            var transaction = new Transaction
            {
                UserId = userId,
                Amount = parsed.Amount,
                Type = parsed.Type,
                Description = string.IsNullOrWhiteSpace(parsed.Description) ? "Quick Entry" : parsed.Description,
                TransactionDate = parsed.TransactionDate,
                CategoryId = parsed.CategoryId.Value,
                PaymentMethodId = parsed.PaymentMethodId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync();
            return true;
        }

        // ── Helper Calculations ───────────────────────────────────────

        private BurnRateForecast CalculateBurnRateForecast(List<Transaction> currentMonthTx, decimal? monthlyBudget, DateTime today)
        {
            var forecast = new BurnRateForecast();
            int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            forecast.DaysElapsed = Math.Max(1, today.Day);
            forecast.DaysRemaining = Math.Max(1, daysInMonth - forecast.DaysElapsed);

            var monthExpenses = currentMonthTx.Where(t => t.Type == "Expense").Sum(t => t.Amount);
            forecast.MonthToDateExpense = monthExpenses;
            forecast.DailyBurnRate = Math.Round(monthExpenses / forecast.DaysElapsed, 2);
            forecast.ProjectedMonthEndExpense = Math.Round(monthExpenses + (forecast.DailyBurnRate * forecast.DaysRemaining), 2);
            forecast.MonthlyBudget = monthlyBudget;

            if (monthlyBudget.HasValue && monthlyBudget.Value > 0)
            {
                decimal remainingBudget = Math.Max(0, monthlyBudget.Value - monthExpenses);
                forecast.SafeDailyBudget = Math.Round(remainingBudget / forecast.DaysRemaining, 2);
                forecast.ProjectedBalance = monthlyBudget.Value - forecast.ProjectedMonthEndExpense;
                forecast.WillExceedBudget = forecast.ProjectedMonthEndExpense > monthlyBudget.Value;

                if (forecast.DailyBurnRate > 0)
                {
                    forecast.DaysUntilBudgetDepletion = Math.Max(0, (int)Math.Floor(remainingBudget / forecast.DailyBurnRate));
                }

                if (forecast.WillExceedBudget)
                    forecast.PaceStatus = "Critical";
                else if (forecast.DailyBurnRate > forecast.SafeDailyBudget * 0.9m)
                    forecast.PaceStatus = "Accelerating";
                else
                    forecast.PaceStatus = "On Track";
            }
            else
            {
                forecast.SafeDailyBudget = forecast.DailyBurnRate;
                forecast.PaceStatus = "On Track";
            }

            return forecast;
        }

        private FinancialHealthScore CalculateFinancialHealthScore(
            List<Transaction> currentMonthTx,
            List<Transaction> prevMonthTx,
            decimal? monthlyBudget,
            BurnRateForecast forecast)
        {
            var score = new FinancialHealthScore();

            decimal currentIncome = currentMonthTx.Where(t => t.Type == "Income").Sum(t => t.Amount);
            decimal currentExpense = currentMonthTx.Where(t => t.Type == "Expense").Sum(t => t.Amount);

            // Pillar 1: Savings Ratio (Target: >= 20% savings)
            if (currentIncome > 0)
            {
                decimal savingsRate = (currentIncome - currentExpense) / currentIncome;
                if (savingsRate >= 0.30m) score.SavingsRatioScore = 100;
                else if (savingsRate >= 0.20m) score.SavingsRatioScore = 85;
                else if (savingsRate >= 0.10m) score.SavingsRatioScore = 70;
                else if (savingsRate >= 0) score.SavingsRatioScore = 55;
                else score.SavingsRatioScore = 25; // Overspending income
            }
            else
            {
                score.SavingsRatioScore = 60; // Neutral if income not recorded
            }

            // Pillar 2: Budget Discipline
            if (monthlyBudget.HasValue && monthlyBudget.Value > 0)
            {
                decimal ratio = currentExpense / monthlyBudget.Value;
                if (ratio <= 0.70m && !forecast.WillExceedBudget) score.BudgetDisciplineScore = 95;
                else if (ratio <= 0.85m && !forecast.WillExceedBudget) score.BudgetDisciplineScore = 85;
                else if (ratio <= 1.0m) score.BudgetDisciplineScore = 65;
                else score.BudgetDisciplineScore = 30;
            }
            else
            {
                score.BudgetDisciplineScore = 70; // Neutral
            }

            // Pillar 3: Spending Stability (compare current burn rate to prev month)
            decimal prevMonthExpense = prevMonthTx.Where(t => t.Type == "Expense").Sum(t => t.Amount);
            if (prevMonthExpense > 0 && forecast.DaysElapsed > 5)
            {
                int prevDaysInMonth = DateTime.DaysInMonth(todayOrPrevMonth(DateTime.Today).Year, todayOrPrevMonth(DateTime.Today).Month);
                decimal prevDailyBurn = prevMonthExpense / prevDaysInMonth;
                decimal diffRatio = Math.Abs(forecast.DailyBurnRate - prevDailyBurn) / prevDailyBurn;

                if (diffRatio <= 0.15m) score.SpendingStabilityScore = 95;
                else if (diffRatio <= 0.30m) score.SpendingStabilityScore = 80;
                else if (diffRatio <= 0.50m) score.SpendingStabilityScore = 65;
                else score.SpendingStabilityScore = 40;
            }
            else
            {
                score.SpendingStabilityScore = 80;
            }

            // Pillar 4: Essential Burden vs Discretionary
            var discretionaryTotal = currentMonthTx
                .Where(t => t.Type == "Expense" && IsDiscretionary(t.Category?.Name ?? ""))
                .Sum(t => t.Amount);

            if (currentExpense > 0)
            {
                decimal discRatio = discretionaryTotal / currentExpense;
                if (discRatio <= 0.30m) score.EssentialBurdenScore = 95;
                else if (discRatio <= 0.45m) score.EssentialBurdenScore = 80;
                else if (discRatio <= 0.60m) score.EssentialBurdenScore = 65;
                else score.EssentialBurdenScore = 45;
            }
            else
            {
                score.EssentialBurdenScore = 85;
            }

            // Pillar 5: Category Balance (No single category dominating > 60%)
            var maxCategorySpend = currentMonthTx
                .Where(t => t.Type == "Expense")
                .GroupBy(t => t.CategoryId)
                .Select(g => g.Sum(x => x.Amount))
                .OrderByDescending(x => x)
                .FirstOrDefault();

            if (currentExpense > 0)
            {
                decimal concentration = maxCategorySpend / currentExpense;
                if (concentration <= 0.40m) score.CategoryBalanceScore = 95;
                else if (concentration <= 0.60m) score.CategoryBalanceScore = 80;
                else score.CategoryBalanceScore = 50;
            }
            else
            {
                score.CategoryBalanceScore = 85;
            }

            // Composite weighted calculation
            double composite = (score.SavingsRatioScore * 0.30) +
                               (score.BudgetDisciplineScore * 0.25) +
                               (score.SpendingStabilityScore * 0.15) +
                               (score.EssentialBurdenScore * 0.15) +
                               (score.CategoryBalanceScore * 0.15);

            score.Score = (int)Math.Clamp(Math.Round(composite), 10, 100);

            if (score.Score >= 90)
            {
                score.Grade = "A+";
                score.StatusBadge = "Elite";
                score.Title = "Exceptional Financial Discipline";
                score.Summary = "Your savings rate and budget adherence are in the top tier. Keep growing your emergency reserves!";
                score.AccentColor = "#00E676";
            }
            else if (score.Score >= 80)
            {
                score.Grade = "A";
                score.StatusBadge = "Strong";
                score.Title = "Healthy & Disciplined";
                score.Summary = "You are in great financial shape with steady savings and controlled daily burn rate.";
                score.AccentColor = "#00E676";
            }
            else if (score.Score >= 70)
            {
                score.Grade = "B";
                score.StatusBadge = "Good";
                score.Title = "Balanced Financial Health";
                score.Summary = "Solid fundamentals, but watch discretionary expenses to maximize monthly surplus.";
                score.AccentColor = "#2979FF";
            }
            else if (score.Score >= 55)
            {
                score.Grade = "C";
                score.StatusBadge = "Fair";
                score.Title = "Needs Optimization";
                score.Summary = "Spending velocity is running slightly high. Reducing dining/shopping will quickly boost your score.";
                score.AccentColor = "#FF9100";
            }
            else
            {
                score.Grade = "D";
                score.StatusBadge = "Critical";
                score.Title = "Attention Required";
                score.Summary = "Expenses are outpacing target limits. Follow the recommended AI action plan to curb burn rate.";
                score.AccentColor = "#FF4C6A";
            }

            return score;
        }

        private List<SpendingAnomaly> DetectAnomalies(List<Transaction> currentMonthTx, List<Transaction> prevMonthTx, BurnRateForecast forecast)
        {
            var anomalies = new List<SpendingAnomaly>();

            // Category surges
            var currentCategorySpend = currentMonthTx
                .Where(t => t.Type == "Expense")
                .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.Icon })
                .Select(g => new { g.Key.CategoryId, g.Key.Name, g.Key.Icon, Total = g.Sum(x => x.Amount) })
                .ToList();

            var prevCategorySpend = prevMonthTx
                .Where(t => t.Type == "Expense")
                .GroupBy(t => t.CategoryId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            foreach (var cat in currentCategorySpend)
            {
                if (prevCategorySpend.TryGetValue(cat.CategoryId, out decimal prevAmount) && prevAmount > 300)
                {
                    double diffPercent = (double)((cat.Total - prevAmount) / prevAmount) * 100;
                    if (diffPercent >= 35.0 && cat.Total > 800)
                    {
                        anomalies.Add(new SpendingAnomaly
                        {
                            Title = $"{cat.Name} Spike Detected",
                            CategoryName = cat.Name,
                            CategoryIcon = cat.Icon ?? "bi-tag",
                            Amount = cat.Total,
                            BaselineAmount = prevAmount,
                            PercentageDiff = Math.Round(diffPercent, 1),
                            Severity = diffPercent >= 75 ? "danger" : "warning",
                            Description = $"Spending in {cat.Name} is running {diffPercent:N0}% higher than last month (₹{cat.Total:N0} vs ₹{prevAmount:N0}).",
                            RecommendedAction = $"Review your recent {cat.Name} receipts and postpone non-essential purchases."
                        });
                    }
                }
            }

            // Over-budget anomaly
            if (forecast.WillExceedBudget && forecast.MonthlyBudget.HasValue)
            {
                anomalies.Add(new SpendingAnomaly
                {
                    Title = "Projected Budget Breach",
                    CategoryName = "Budget",
                    CategoryIcon = "bi-exclamation-octagon",
                    Amount = forecast.ProjectedMonthEndExpense,
                    BaselineAmount = forecast.MonthlyBudget.Value,
                    PercentageDiff = (double)Math.Round(((forecast.ProjectedMonthEndExpense - forecast.MonthlyBudget.Value) / forecast.MonthlyBudget.Value) * 100, 1),
                    Severity = "danger",
                    Description = $"At current daily spend (₹{forecast.DailyBurnRate:N0}/day), you will exceed your monthly budget by ₹{Math.Abs(forecast.ProjectedBalance):N0}.",
                    RecommendedAction = $"Cap daily variable spending to ₹{forecast.SafeDailyBudget:N0}/day for the remaining {forecast.DaysRemaining} days."
                });
            }

            return anomalies;
        }

        private List<AiSmartSuggestion> GenerateSmartSuggestions(
            List<Transaction> currentMonthTx,
            FinancialHealthScore health,
            BurnRateForecast forecast,
            List<SpendingAnomaly> anomalies)
        {
            var list = new List<AiSmartSuggestion>();

            if (forecast.WillExceedBudget)
            {
                list.Add(new AiSmartSuggestion
                {
                    Title = "Activate Safe Daily Spend Limit",
                    Description = $"Restrict daily spend to ₹{forecast.SafeDailyBudget:N2}/day to prevent exceeding your budget this month.",
                    Icon = "bi-shield-check",
                    ImpactBadge = "High Impact",
                    ActionType = "budget",
                    ActionUrl = "/Budget"
                });
            }

            var topDiscretionary = currentMonthTx
                .Where(t => t.Type == "Expense" && IsDiscretionary(t.Category?.Name ?? ""))
                .GroupBy(t => t.Category.Name)
                .OrderByDescending(g => g.Sum(x => x.Amount))
                .FirstOrDefault();

            if (topDiscretionary != null)
            {
                decimal total = topDiscretionary.Sum(x => x.Amount);
                list.Add(new AiSmartSuggestion
                {
                    Title = $"Optimize {topDiscretionary.Key} Spend",
                    Description = $"You've spent ₹{total:N0} on {topDiscretionary.Key}. Trimming 15% will add ₹{Math.Round(total * 0.15m, 0):N0} to your month-end savings.",
                    Icon = "bi-graph-down-arrow",
                    ImpactBadge = "Medium Impact",
                    ActionType = "general"
                });
            }

            list.Add(new AiSmartSuggestion
            {
                Title = "Automate Recurring Expense Tracking",
                Description = "Keep your fixed bills on schedule and avoid surprise charges by configuring recurring transactions.",
                Icon = "bi-arrow-repeat",
                ImpactBadge = "Preventative",
                ActionType = "recurring",
                ActionUrl = "/RecurringExpenses"
            });

            return list;
        }

        private List<CategorySpendingTrend> GenerateCategoryTrends(List<Transaction> currentMonthTx, List<Transaction> prevMonthTx)
        {
            var prevDict = prevMonthTx
                .Where(t => t.Type == "Expense")
                .GroupBy(t => t.CategoryId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            return currentMonthTx
                .Where(t => t.Type == "Expense")
                .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.Icon, t.Category.Color })
                .Select(g =>
                {
                    decimal currentTotal = g.Sum(x => x.Amount);
                    prevDict.TryGetValue(g.Key.CategoryId, out decimal prevTotal);
                    double growth = prevTotal > 0 ? (double)((currentTotal - prevTotal) / prevTotal) * 100 : 0;

                    return new CategorySpendingTrend
                    {
                        CategoryName = g.Key.Name,
                        CategoryIcon = g.Key.Icon ?? "bi-tag",
                        CategoryColor = g.Key.Color ?? "#6366F1",
                        CurrentMonthAmount = currentTotal,
                        PreviousMonthAmount = prevTotal,
                        GrowthPercentage = Math.Round(growth, 1)
                    };
                })
                .OrderByDescending(x => x.CurrentMonthAmount)
                .Take(6)
                .ToList();
        }

        private static bool IsDiscretionary(string categoryName)
        {
            var discretionary = new[] { "dining", "food", "entertainment", "shopping", "leisure", "outing", "cafe", "coffee", "movies", "games" };
            return discretionary.Any(d => categoryName.Contains(d, StringComparison.OrdinalIgnoreCase));
        }

        private static DateTime todayOrPrevMonth(DateTime today) => today.AddMonths(-1);
    }
}
