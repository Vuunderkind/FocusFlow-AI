namespace FocusFlow_LMS.Models
{
    public class Conversation
    {
        public int    Id         { get; set; }
        public string Title      { get; set; } = "Новый чат";
        public string AgentId    { get; set; } = "default";
        public string Model      { get; set; } = "claude-opus-4-6";
        public bool   IsPinned   { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // For UI display only
        public string? LastMessage { get; set; }
        public int     MessageCount { get; set; }
    }
}
