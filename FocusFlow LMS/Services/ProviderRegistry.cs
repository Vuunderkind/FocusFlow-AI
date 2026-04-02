using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Services
{
    // ── Registry of all configured AI providers ───────────────────
    public class ProviderRegistry
    {
        private readonly AppConfig                        _config;
        private readonly Dictionary<ProviderType, ILLMProvider> _providers;

        public ProviderRegistry(AppConfig config)
        {
            _config = config;
            _providers = new()
            {
                [ProviderType.Anthropic] = new AnthropicProvider(() => _config.AnthropicApiKey),
                [ProviderType.OpenAI]    = new OpenAICompatProvider(
                    ProviderType.OpenAI,
                    "https://api.openai.com/v1/chat/completions",
                    () => _config.OpenAiApiKey),
                [ProviderType.Google]    = new GeminiProvider(() => _config.GeminiApiKey),
                [ProviderType.Mistral]   = new OpenAICompatProvider(
                    ProviderType.Mistral,
                    "https://api.mistral.ai/v1/chat/completions",
                    () => _config.MistralApiKey),
                [ProviderType.Groq]      = new OpenAICompatProvider(
                    ProviderType.Groq,
                    "https://api.groq.com/openai/v1/chat/completions",
                    () => _config.GroqApiKey),
            };
        }

        public ILLMProvider? GetProvider(ProviderType type)
            => _providers.TryGetValue(type, out var p) ? p : null;

        public ILLMProvider? GetProvider(ModelInfo model)
            => GetProvider(model.Provider);

        // Returns all models for which the provider is configured
        public List<ModelInfo> GetAvailableModels()
            => ModelInfo.All
                .Where(m => _providers.TryGetValue(m.Provider, out var p) && p.IsConfigured)
                .ToList();

        public bool AnyConfigured() => _providers.Values.Any(p => p.IsConfigured);

        // Reload config (after settings save)
        public void Reload(AppConfig config)
        {
            (_providers[ProviderType.Anthropic] as AnthropicProvider)?.GetType(); // just touch
            // Simplest approach: recreate (called from outside)
        }

        public async Task<AIResponse> SendAsync(
            ModelInfo         model,
            string            systemPrompt,
            List<ChatMessage> history,
            string            userMessage,
            float             temperature  = 0.7f,
            int               maxTokens    = 4096,
            CancellationToken ct           = default)
        {
            var provider = GetProvider(model);
            if (provider == null)
                return new AIResponse { Success = false, Error = $"Провайдер {model.Provider} не найден." };
            if (!provider.IsConfigured)
                return new AIResponse { Success = false, Error = $"{model.Provider} API ключ не настроен. Добавьте его в Настройках." };

            return await provider.SendAsync(systemPrompt, history, userMessage,
                model.ApiModel, temperature, maxTokens, ct);
        }
    }
}
