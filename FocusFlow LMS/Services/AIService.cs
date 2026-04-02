using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Services
{
    public class ChatMessage
    {
        [JsonProperty("role")]
        public string Role    { get; set; } = "user";
        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class AIResponse
    {
        public string  Text         { get; set; } = string.Empty;
        public int     InputTokens  { get; set; }
        public int     OutputTokens { get; set; }
        public string? Model        { get; set; }
        public string? Provider     { get; set; }
        public bool    Success      { get; set; }
        public string? Error        { get; set; }
    }

    // ── Thin compatibility façade (used by WorkflowService & agents) ──
    public class AIService
    {
        private readonly AppConfig        _config;
        private readonly ProviderRegistry _registry;

        public AIService(AppConfig config)
        {
            _config   = config;
            _registry = new ProviderRegistry(config);
        }

        public ProviderRegistry Registry => _registry;

        public async Task<AIResponse> SendAsync(
            string             systemPrompt,
            List<ChatMessage>  history,
            string             userMessage,
            string?            modelId    = null,
            float              temperature = 0.7f,
            int                maxTokens   = 4096,
            CancellationToken  ct          = default)
        {
            var mid   = modelId ?? _config.DefaultModel;
            var model = ModelInfo.FindById(mid) ?? ModelInfo.FindById("claude-opus-4-6");

            if (model == null)
            {
                // Fallback: use any configured model
                var avail = _registry.GetAvailableModels();
                if (!avail.Any())
                    return new AIResponse { Success = false, Error = "Нет настроенных провайдеров. Добавьте API ключи в Настройках." };
                model = avail[0];
            }

            return await _registry.SendAsync(model, systemPrompt, history, userMessage,
                temperature, maxTokens, ct);
        }

        public async Task<string> GenerateTitleAsync(string firstMsg, CancellationToken ct = default)
        {
            var avail = _registry.GetAvailableModels();
            // Pick fastest/cheapest available
            var model = avail.Where(m => m.IsFast).OrderBy(m => m.CostPer1K).FirstOrDefault()
                     ?? avail.FirstOrDefault();
            if (model == null) return "Новый чат";

            var resp = await _registry.SendAsync(
                model,
                "Придумай очень короткое название (3–6 слов) для чата. Только название, без кавычек и пояснений.",
                [], firstMsg, 0.3f, 30, ct);

            return resp.Success && !string.IsNullOrWhiteSpace(resp.Text)
                ? resp.Text.Trim().TrimEnd('.', ',')
                : "Новый чат";
        }
    }
}
