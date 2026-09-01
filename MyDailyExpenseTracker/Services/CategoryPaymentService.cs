using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyDailyExpenseTracker.Data;
using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Services
{
    /// <summary>
    /// Manages categories for a user.
    /// Users see their own custom categories PLUS the system default categories.
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _db;

        public CategoryService(ApplicationDbContext db) => _db = db;

        private IQueryable<Category> UserCategories(string userId) =>
            _db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && (c.UserId == userId || c.UserId == null));

        public async Task<List<CategoryViewModel>> GetCategoriesAsync(string userId)
        {
            var txCounts = await _db.Transactions
                .Where(t => t.UserId == userId)
                .GroupBy(t => t.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CategoryId, g => g.Count);

            return await UserCategories(userId)
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryViewModel
                {
                    CategoryId = c.CategoryId,
                    Name       = c.Name,
                    Icon       = c.Icon,
                    Color      = c.Color,
                    Type       = c.Type,
                    IsDefault  = c.IsDefault
                })
                .ToListAsync()
                .ContinueWith(t =>
                {
                    foreach (var vm in t.Result)
                        vm.TransactionCount = txCounts.GetValueOrDefault(vm.CategoryId);
                    return t.Result;
                });
        }

        public async Task<CategoryViewModel?> GetCategoryByIdAsync(int id, string userId)
        {
            var c = await UserCategories(userId)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            if (c == null) return null;

            return new CategoryViewModel
            {
                CategoryId       = c.CategoryId,
                Name             = c.Name,
                Icon             = c.Icon,
                Color            = c.Color,
                Type             = c.Type,
                IsDefault        = c.IsDefault,
                TransactionCount = await _db.Transactions.CountAsync(t => t.UserId == userId && t.CategoryId == id)
            };
        }

        public async Task<int> CreateCategoryAsync(string userId, CategoryViewModel vm)
        {
            var category = new Category
            {
                UserId      = userId,
                Name        = vm.Name.Trim(),
                Icon        = vm.Icon,
                Color       = vm.Color,
                Type        = vm.Type,
                IsDefault   = false,
                IsActive    = true,
                CreatedDate = DateTime.UtcNow
            };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return category.CategoryId;
        }

        public async Task<bool> UpdateCategoryAsync(string userId, CategoryViewModel vm)
        {
            // Only allow editing user's own categories, not system defaults
            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == vm.CategoryId && c.UserId == userId);
            if (category == null) return false;

            category.Name  = vm.Name.Trim();
            category.Icon  = vm.Icon;
            category.Color = vm.Color;
            category.Type  = vm.Type;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id, string userId)
        {
            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.UserId == userId);
            if (category == null) return false;

            // Safety: check if category is in use
            var inUse = await _db.Transactions.AnyAsync(t => t.UserId == userId && t.CategoryId == id);
            if (inUse) return false;  // Caller should show error message

            category.IsActive = false;  // Soft delete
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<SelectListItem>> GetCategorySelectListAsync(string userId, string? type = null)
        {
            var query = UserCategories(userId);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(c => c.Type == type || c.Type == "Both");

            return await query
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                .ToListAsync();
        }
    }

    /// <summary>
    /// Manages payment methods for a user.
    /// Users see their own methods PLUS system defaults.
    /// </summary>
    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly ApplicationDbContext _db;

        public PaymentMethodService(ApplicationDbContext db) => _db = db;

        private IQueryable<PaymentMethod> UserMethods(string userId) =>
            _db.PaymentMethods
                .AsNoTracking()
                .Where(pm => pm.IsActive && (pm.UserId == userId || pm.UserId == null));

        public async Task<List<PaymentMethodViewModel>> GetPaymentMethodsAsync(string userId)
        {
            var txCounts = await _db.Transactions
                .Where(t => t.UserId == userId && t.PaymentMethodId != null)
                .GroupBy(t => t.PaymentMethodId)
                .Select(g => new { MethodId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.MethodId!.Value, g => g.Count);

            return await UserMethods(userId)
                .OrderBy(pm => pm.Name)
                .Select(pm => new PaymentMethodViewModel
                {
                    PaymentMethodId  = pm.PaymentMethodId,
                    Name             = pm.Name,
                    Icon             = pm.Icon,
                    IsDefault        = pm.IsDefault,
                    TransactionCount = txCounts.GetValueOrDefault(pm.PaymentMethodId)
                })
                .ToListAsync();
        }

        public async Task<PaymentMethodViewModel?> GetPaymentMethodByIdAsync(int id, string userId)
        {
            var pm = await UserMethods(userId).FirstOrDefaultAsync(p => p.PaymentMethodId == id);
            if (pm == null) return null;
            return new PaymentMethodViewModel
            {
                PaymentMethodId  = pm.PaymentMethodId,
                Name             = pm.Name,
                Icon             = pm.Icon,
                IsDefault        = pm.IsDefault,
                TransactionCount = await _db.Transactions.CountAsync(t => t.UserId == userId && t.PaymentMethodId == id)
            };
        }

        public async Task<int> CreatePaymentMethodAsync(string userId, PaymentMethodViewModel vm)
        {
            var pm = new PaymentMethod
            {
                UserId      = userId,
                Name        = vm.Name.Trim(),
                Icon        = vm.Icon,
                IsDefault   = false,
                IsActive    = true,
                CreatedDate = DateTime.UtcNow
            };
            _db.PaymentMethods.Add(pm);
            await _db.SaveChangesAsync();
            return pm.PaymentMethodId;
        }

        public async Task<bool> UpdatePaymentMethodAsync(string userId, PaymentMethodViewModel vm)
        {
            var pm = await _db.PaymentMethods
                .FirstOrDefaultAsync(p => p.PaymentMethodId == vm.PaymentMethodId && p.UserId == userId);
            if (pm == null) return false;
            pm.Name = vm.Name.Trim();
            pm.Icon = vm.Icon;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeletePaymentMethodAsync(int id, string userId)
        {
            var pm = await _db.PaymentMethods
                .FirstOrDefaultAsync(p => p.PaymentMethodId == id && p.UserId == userId);
            if (pm == null) return false;

            var inUse = await _db.Transactions.AnyAsync(t => t.UserId == userId && t.PaymentMethodId == id);
            if (inUse) return false;

            pm.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<SelectListItem>> GetPaymentMethodSelectListAsync(string userId)
        {
            return await UserMethods(userId)
                .OrderBy(pm => pm.Name)
                .Select(pm => new SelectListItem { Value = pm.PaymentMethodId.ToString(), Text = pm.Name })
                .ToListAsync();
        }
    }
}
