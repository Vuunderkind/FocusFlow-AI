namespace FocusFlow_LMS.Models
{
    public enum MessageRole { User, Assistant, System }

    public class AiMessage
    {
        public int         Id             { get; set; }
        public int         ConversationId { get; set; }
        public MessageRole Role           { get; set; }
        public string      Content        { get; set; } = string.Empty;
        public DateTime    CreatedAt      { get; set; } = DateTime.Now;
        public int         TokensUsed     { get; set; }
        public string?     ModelUsed      { get; set; }
        public bool        IsError        { get; set; } = false;
    }
}
