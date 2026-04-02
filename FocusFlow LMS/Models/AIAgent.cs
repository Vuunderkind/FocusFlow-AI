namespace FocusFlow_LMS.Models
{
    public class AIAgent
    {
        public string   Id           { get; set; } = Guid.NewGuid().ToString();
        public string   Name         { get; set; } = string.Empty;
        public string   Description  { get; set; } = string.Empty;
        public string   SystemPrompt { get; set; } = string.Empty;
        public string   Emoji        { get; set; } = "🤖";
        public string   ColorHex     { get; set; } = "#7C5CFC";
        public string   Model        { get; set; } = "claude-opus-4-6";
        public float    Temperature  { get; set; } = 0.7f;
        public int      MaxTokens    { get; set; } = 4096;
        public bool     IsBuiltIn    { get; set; } = false;
        public DateTime CreatedAt    { get; set; } = DateTime.Now;

        public Color    Color => ParseColor(ColorHex);

        private static Color ParseColor(string hex)
        {
            try { return ColorTranslator.FromHtml(hex); }
            catch { return Color.FromArgb(124, 92, 252); }
        }
    }
}
