using System.Threading.Tasks;

namespace API.Services
{
    public interface ILlmService
    {
        Task<(string Category, string DraftResponse, int UrgencyScore)> AnalyzeContactSubmissionAsync(string subject, string message);
        Task<(string Description, string Aftercare)> GenerateServiceContentAsync(string serviceName, decimal price);
        Task<string> SummarizeClientHistoryAsync(int clientId);
    }
}
