using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MyDailyExpenseTracker.Data;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.ViewModels;
using System.Text;

namespace MyDailyExpenseTracker.Services
{
    /// <summary>
    /// Handles CRUD operations and export for Transactions.
    /// All methods are scoped to the authenticated userId — no cross-user data leakage.
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICategoryService _categoryService;
        private readonly IPaymentMethodService _pmService;

        public TransactionService(
            ApplicationDbContext db,
            ICategoryService categoryService,
            IPaymentMethodService pmService)
        {
            _db             = db;
            _categoryService = categoryService;
            _pmService       = pmService;
        }

        // ── Read: filtered + paginated list ───────────────────────────
        public async Task<TransactionFilterViewModel> GetFilteredTransactionsAsync(
            string userId, TransactionFilterViewModel filter)
        {
            var query = _db.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.PaymentMethod)
                .Where(t => t.UserId == userId);

            // Apply filters
            if (filter.FromDate.HasValue)
                query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(t => t.TransactionDate <= filter.ToDate.Value);
            if (filter.CategoryId.HasValue)
                query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
            if (filter.PaymentMethodId.HasValue)
                query = query.Where(t => t.PaymentMethodId == filter.PaymentMethodId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Type))
                query = query.Where(t => t.Type == filter.Type);
            if (filter.MinAmount.HasValue)
                query = query.Where(t => t.Amount >= filter.MinAmount.Value);
            if (filter.MaxAmount.HasValue)
                query = query.Where(t => t.Amount <= filter.MaxAmount.Value);
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
                query = query.Where(t =>
                    t.Description.Contains(filter.SearchText) ||
                    (t.Notes != null && t.Notes.Contains(filter.SearchText)));

            // Summary totals before pagination
            var allFiltered = await query
                .Select(t => new { t.Type, t.Amount })
                .ToListAsync();

            filter.FilteredTotalExpense = allFiltered.Where(t => t.Type == "Expense").Sum(t => t.Amount);
            filter.FilteredTotalIncome  = allFiltered.Where(t => t.Type == "Income").Sum(t => t.Amount);
            filter.TotalCount           = allFiltered.Count;

            // Sorting
            query = (filter.SortBy, filter.SortOrder) switch
            {
                ("Date",   "asc")  => query.OrderBy(t => t.TransactionDate).ThenBy(t => t.CreatedDate),
                ("Date",   _)      => query.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedDate),
                ("Amount", "asc")  => query.OrderBy(t => t.Amount),
                ("Amount", _)      => query.OrderByDescending(t => t.Amount),
                ("Description", _) => query.OrderBy(t => t.Description),
                _                  => query.OrderByDescending(t => t.TransactionDate)
            };

            // Pagination
            var skip = (filter.Page - 1) * filter.PageSize;
            var transactions = await query
                .Skip(skip)
                .Take(filter.PageSize)
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

            filter.Transactions    = transactions;
            filter.Categories      = await _categoryService.GetCategorySelectListAsync(userId);
            filter.PaymentMethods  = await _pmService.GetPaymentMethodSelectListAsync(userId);
            return filter;
        }

        // ── Read: single transaction by ID ────────────────────────────
        public async Task<TransactionViewModel?> GetTransactionByIdAsync(int id, string userId)
        {
            var tx = await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.PaymentMethod)
                .FirstOrDefaultAsync(t => t.TransactionId == id && t.UserId == userId);

            if (tx == null) return null;

            return new TransactionViewModel
            {
                TransactionId     = tx.TransactionId,
                Type              = tx.Type,
                Amount            = tx.Amount,
                CategoryId        = tx.CategoryId,
                CategoryName      = tx.Category.Name,
                PaymentMethodId   = tx.PaymentMethodId,
                PaymentMethodName = tx.PaymentMethod?.Name,
                Description       = tx.Description,
                Notes             = tx.Notes,
                TransactionDate   = tx.TransactionDate,
                CreatedDate       = tx.CreatedDate,
                UpdatedDate       = tx.UpdatedDate,
                Categories        = await _categoryService.GetCategorySelectListAsync(userId, tx.Type),
                PaymentMethods    = await _pmService.GetPaymentMethodSelectListAsync(userId)
            };
        }

        // ── Create ────────────────────────────────────────────────────
        public async Task<int> CreateTransactionAsync(string userId, TransactionViewModel vm)
        {
            var tx = new Transaction
            {
                UserId          = userId,
                Type            = vm.Type,
                Amount          = vm.Amount,
                CategoryId      = vm.CategoryId,
                PaymentMethodId = vm.PaymentMethodId,
                Description     = vm.Description.Trim(),
                Notes           = vm.Notes?.Trim(),
                TransactionDate = vm.TransactionDate,
                CreatedDate     = DateTime.UtcNow,
                UpdatedDate     = DateTime.UtcNow
            };
            _db.Transactions.Add(tx);
            await _db.SaveChangesAsync();
            return tx.TransactionId;
        }

        // ── Update ────────────────────────────────────────────────────
        public async Task<bool> UpdateTransactionAsync(string userId, TransactionViewModel vm)
        {
            var tx = await _db.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == vm.TransactionId && t.UserId == userId);
            if (tx == null) return false;

            tx.Type            = vm.Type;
            tx.Amount          = vm.Amount;
            tx.CategoryId      = vm.CategoryId;
            tx.PaymentMethodId = vm.PaymentMethodId;
            tx.Description     = vm.Description.Trim();
            tx.Notes           = vm.Notes?.Trim();
            tx.TransactionDate = vm.TransactionDate;
            tx.UpdatedDate     = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        // ── Delete ────────────────────────────────────────────────────
        public async Task<bool> DeleteTransactionAsync(int id, string userId)
        {
            var tx = await _db.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == id && t.UserId == userId);
            if (tx == null) return false;

            _db.Transactions.Remove(tx);
            await _db.SaveChangesAsync();
            return true;
        }

        // ── Recent Transactions ───────────────────────────────────────
        public async Task<List<TransactionListItem>> GetRecentTransactionsAsync(string userId, int count = 10)
        {
            return await _db.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.PaymentMethod)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedDate)
                .Take(count)
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
        }

        // ── Export: CSV ───────────────────────────────────────────────
        public async Task ExportToCsvAsync(string userId, TransactionFilterViewModel filter, Stream outputStream)
        {
            // Re-run query without pagination
            filter.Page     = 1;
            filter.PageSize = int.MaxValue;
            var result = await GetFilteredTransactionsAsync(userId, filter);

            var sb = new StringBuilder();
            sb.AppendLine("Date,Description,Category,Type,Amount,Payment Method");
            foreach (var t in result.Transactions)
            {
                sb.AppendLine($"{t.TransactionDate:dd-MM-yyyy}," +
                    $"\"{t.Description}\"," +
                    $"\"{t.CategoryName}\"," +
                    $"{t.Type}," +
                    $"{t.Amount}," +
                    $"\"{t.PaymentMethodName}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            await outputStream.WriteAsync(bytes);
        }

        // ── Export: Excel ─────────────────────────────────────────────
        public async Task ExportToExcelAsync(string userId, TransactionFilterViewModel filter, Stream outputStream)
        {
            filter.Page     = 1;
            filter.PageSize = int.MaxValue;
            var result = await GetFilteredTransactionsAsync(userId, filter);

            using var workbook  = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Transactions");

            // Header row
            var headers = new[] { "Date", "Description", "Category", "Type", "Amount (₹)", "Payment Method" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                cell.Style.Font.FontColor       = XLColor.White;
            }

            // Data rows
            int row = 2;
            foreach (var t in result.Transactions)
            {
                worksheet.Cell(row, 1).Value = t.TransactionDate.ToString("dd-MM-yyyy");
                worksheet.Cell(row, 2).Value = t.Description;
                worksheet.Cell(row, 3).Value = t.CategoryName;
                worksheet.Cell(row, 4).Value = t.Type;
                worksheet.Cell(row, 5).Value = t.Amount;
                worksheet.Cell(row, 6).Value = t.PaymentMethodName ?? "-";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(outputStream);
        }
    }
}
