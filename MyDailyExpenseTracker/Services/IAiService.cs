using MyDailyExpenseTracker.ViewModels;

namespace MyDailyExpenseTracker.Services
{
    public interface IAiService
    {
        Task<AiInsightsViewModel> GetAiInsightsDashboardAsync(string userId);
        Task<AiChatResponse> AskFinancialCopilotAsync(string userId, string query);
        Task<AiParsedTransaction> ParseNaturalLanguageTransactionAsync(string userId, string input);
        Task<bool> SaveParsedTransactionAsync(string userId, AiParsedTransaction parsed);
    }
}
