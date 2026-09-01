using MyDailyExpenseTracker.Models;
using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Services
{
    public interface ITransactionService
    {
        Task<TransactionFilterViewModel> GetFilteredTransactionsAsync(string userId, TransactionFilterViewModel filter);
        Task<TransactionViewModel?> GetTransactionByIdAsync(int id, string userId);
        Task<int> CreateTransactionAsync(string userId, TransactionViewModel vm);
        Task<bool> UpdateTransactionAsync(string userId, TransactionViewModel vm);
        Task<bool> DeleteTransactionAsync(int id, string userId);
        Task<List<TransactionListItem>> GetRecentTransactionsAsync(string userId, int count = 10);
        Task ExportToCsvAsync(string userId, TransactionFilterViewModel filter, Stream outputStream);
        Task ExportToExcelAsync(string userId, TransactionFilterViewModel filter, Stream outputStream);
    }

    public interface ICategoryService
    {
        Task<List<CategoryViewModel>> GetCategoriesAsync(string userId);
        Task<CategoryViewModel?> GetCategoryByIdAsync(int id, string userId);
        Task<int> CreateCategoryAsync(string userId, CategoryViewModel vm);
        Task<bool> UpdateCategoryAsync(string userId, CategoryViewModel vm);
        Task<bool> DeleteCategoryAsync(int id, string userId);
        Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetCategorySelectListAsync(string userId, string? type = null);
    }

    public interface IPaymentMethodService
    {
        Task<List<PaymentMethodViewModel>> GetPaymentMethodsAsync(string userId);
        Task<PaymentMethodViewModel?> GetPaymentMethodByIdAsync(int id, string userId);
        Task<int> CreatePaymentMethodAsync(string userId, PaymentMethodViewModel vm);
        Task<bool> UpdatePaymentMethodAsync(string userId, PaymentMethodViewModel vm);
        Task<bool> DeletePaymentMethodAsync(int id, string userId);
        Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetPaymentMethodSelectListAsync(string userId);
    }

    public interface IBudgetService
    {
        Task<BudgetViewModel> GetOrCreateBudgetAsync(string userId, int month, int year);
        Task<bool> SaveBudgetAsync(string userId, BudgetViewModel vm);
        Task<decimal?> GetMonthlyBudgetAmountAsync(string userId, int month, int year);
    }

    public interface IReportService
    {
        Task<ReportViewModel> GetMonthlyReportAsync(string userId, int month, int year);
    }

    public interface INotificationService
    {
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task GenerateBudgetNotificationsAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
    }

    public interface IRecurringExpenseService
    {
        Task<List<RecurringExpenseViewModel>> GetRecurringExpensesAsync(string userId);
        Task<RecurringExpenseViewModel?> GetByIdAsync(int id, string userId);
        Task<int> CreateAsync(string userId, RecurringExpenseViewModel vm);
        Task<bool> UpdateAsync(string userId, RecurringExpenseViewModel vm);
        Task<bool> DeleteAsync(int id, string userId);
        Task GenerateDueTransactionsAsync(string userId);
    }

    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(string userId);
    }
}
