using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Services
{
    // ── Unified interface for all LLM providers ───────────────────
    public interface ILLMProvider
    {
        ProviderType ProviderType { get; }
        bool         IsConfigured { get; }

        Task<AIResponse> SendAsync(
            string            systemPrompt,
            List<ChatMessage> history,
            string            userMessage,
            string            modelApiName,
            float             temperature  = 0.7f,
            int               maxTokens    = 4096,
            CancellationToken ct           = default);
    }
}
