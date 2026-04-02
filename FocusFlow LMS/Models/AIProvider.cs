namespace FocusFlow_LMS.Models
{
    public enum ProviderType
    {
        Anthropic,
        OpenAI,
        Google,
        Mistral,
        Groq,
    }

    public enum QueryType
    {
        Code,
        Creative,
        Analysis,
        Math,
        Research,
        Simple,
        Unknown,
    }

    public enum OrchestrationMode
    {
        Auto,       // AI Router выбирает лучшую модель автоматически
        Fusion,     // Несколько AI отвечают → синтез лучшего ответа
        Manual,     // Пользователь выбирает вручную
    }

    public class ModelInfo
    {
        public string       Id           { get; init; } = string.Empty;
        public string       DisplayName  { get; init; } = string.Empty;
        public ProviderType Provider     { get; init; }
        public string       ApiModel     { get; init; } = string.Empty;  // actual API name
        public int          ContextWindow{ get; init; } = 4096;
        public float        CostPer1K    { get; init; } = 0.001f;  // $ per 1K output tokens
        public bool         IsCodeExpert { get; init; }
        public bool         IsCreative   { get; init; }
        public bool         IsFast       { get; init; }
        public bool         IsAnalysis   { get; init; }
        public bool         IsMath       { get; init; }
        public int          QualityScore { get; init; } = 5;  // 1-10
        public string       Emoji        { get; init; } = "🤖";

        // ── Built-in catalog ────────────────────────────────────
        public static readonly List<ModelInfo> All = new()
        {
            // ── Anthropic Claude ──────────────────────────────────
            new()
            {
                Id = "claude-opus-4-6", DisplayName = "Claude Opus 4.6",
                Provider = ProviderType.Anthropic, ApiModel = "claude-opus-4-6",
                ContextWindow = 200000, CostPer1K = 0.075f,
                IsCodeExpert = true, IsCreative = true, IsAnalysis = true,
                QualityScore = 10, Emoji = "✨",
            },
            new()
            {
                Id = "claude-sonnet-4-6", DisplayName = "Claude Sonnet 4.6",
                Provider = ProviderType.Anthropic, ApiModel = "claude-sonnet-4-6",
                ContextWindow = 200000, CostPer1K = 0.015f,
                IsCodeExpert = true, IsAnalysis = true,
                QualityScore = 8, Emoji = "✨",
            },
            new()
            {
                Id = "claude-haiku-4-5", DisplayName = "Claude Haiku 4.5",
                Provider = ProviderType.Anthropic, ApiModel = "claude-haiku-4-5-20251001",
                ContextWindow = 200000, CostPer1K = 0.00125f,
                IsFast = true,
                QualityScore = 6, Emoji = "✨",
            },

            // ── OpenAI ────────────────────────────────────────────
            new()
            {
                Id = "gpt-4o", DisplayName = "GPT-4o",
                Provider = ProviderType.OpenAI, ApiModel = "gpt-4o",
                ContextWindow = 128000, CostPer1K = 0.015f,
                IsCodeExpert = true, IsMath = true, IsAnalysis = true,
                QualityScore = 9, Emoji = "🟢",
            },
            new()
            {
                Id = "gpt-4o-mini", DisplayName = "GPT-4o Mini",
                Provider = ProviderType.OpenAI, ApiModel = "gpt-4o-mini",
                ContextWindow = 128000, CostPer1K = 0.0006f,
                IsFast = true, IsMath = true,
                QualityScore = 6, Emoji = "🟢",
            },

            // ── Google Gemini ─────────────────────────────────────
            new()
            {
                Id = "gemini-2-flash", DisplayName = "Gemini 2.0 Flash",
                Provider = ProviderType.Google, ApiModel = "gemini-2.0-flash",
                ContextWindow = 1000000, CostPer1K = 0.0004f,
                IsFast = true, IsMath = true,
                QualityScore = 7, Emoji = "💎",
            },
            new()
            {
                Id = "gemini-1.5-pro", DisplayName = "Gemini 1.5 Pro",
                Provider = ProviderType.Google, ApiModel = "gemini-1.5-pro-latest",
                ContextWindow = 2000000, CostPer1K = 0.007f,
                IsAnalysis = true, IsMath = true, IsCreative = true,
                QualityScore = 8, Emoji = "💎",
            },

            // ── Mistral ───────────────────────────────────────────
            new()
            {
                Id = "mistral-large", DisplayName = "Mistral Large",
                Provider = ProviderType.Mistral, ApiModel = "mistral-large-latest",
                ContextWindow = 128000, CostPer1K = 0.006f,
                IsCodeExpert = true, IsAnalysis = true,
                QualityScore = 8, Emoji = "🌪",
            },
            new()
            {
                Id = "mistral-small", DisplayName = "Mistral Small",
                Provider = ProviderType.Mistral, ApiModel = "mistral-small-latest",
                ContextWindow = 128000, CostPer1K = 0.0006f,
                IsFast = true,
                QualityScore = 5, Emoji = "🌪",
            },

            // ── Groq (ultra-fast inference) ───────────────────────
            new()
            {
                Id = "groq-llama3-70b", DisplayName = "Llama 3.3 70B (Groq)",
                Provider = ProviderType.Groq, ApiModel = "llama-3.3-70b-versatile",
                ContextWindow = 128000, CostPer1K = 0.0006f,
                IsFast = true, IsCodeExpert = true,
                QualityScore = 7, Emoji = "⚡",
            },
            new()
            {
                Id = "groq-llama3-8b", DisplayName = "Llama 3.1 8B (Groq)",
                Provider = ProviderType.Groq, ApiModel = "llama-3.1-8b-instant",
                ContextWindow = 128000, CostPer1K = 0.00006f,
                IsFast = true,
                QualityScore = 4, Emoji = "⚡",
            },
        };

        public static ModelInfo? FindById(string id)
            => All.FirstOrDefault(m => m.Id == id);

        public static List<ModelInfo> ByProvider(ProviderType p)
            => All.Where(m => m.Provider == p).ToList();
    }

    // ── Route decision returned by AIRouter ──────────────────────
    public class RouteDecision
    {
        public QueryType           QueryType   { get; set; }
        public OrchestrationMode   Mode        { get; set; }
        public List<ModelInfo>     Models      { get; set; } = new();
        public string              Reasoning   { get; set; } = string.Empty;
        public ModelInfo?          Primary     => Models.FirstOrDefault();
    }
}
