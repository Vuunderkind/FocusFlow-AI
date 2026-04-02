using Newtonsoft.Json;

namespace FocusFlow_LMS.Models
{
    public class AppConfig
    {
        // ── API Keys (all providers) ─────────────────────────────
        public string AnthropicApiKey { get; set; } = string.Empty;
        public string OpenAiApiKey    { get; set; } = string.Empty;
        public string GeminiApiKey    { get; set; } = string.Empty;
        public string MistralApiKey   { get; set; } = string.Empty;
        public string GroqApiKey      { get; set; } = string.Empty;

        // ── Orchestration settings ───────────────────────────────
        public string             DefaultModel         { get; set; } = "claude-opus-4-6";
        public OrchestrationMode  DefaultOrchestration { get; set; } = OrchestrationMode.Auto;

        // ── General ──────────────────────────────────────────────
        public int    MaxHistoryMessages { get; set; } = 50;
        public float  Temperature        { get; set; } = 0.7f;
        public int    MaxTokens          { get; set; } = 4096;
        public bool   AutoTitleChats     { get; set; } = true;
        public bool   ShowRouterInfo     { get; set; } = true;   // show "AUTO chose X" badge

        private static readonly string ConfigPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "FocusFlowAI", "config.json");

        public static AppConfig Load()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (!File.Exists(ConfigPath)) return new AppConfig();
                var json = File.ReadAllText(ConfigPath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
            catch { return new AppConfig(); }
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch { /* silent */ }
        }
    }
}
