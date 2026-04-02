using Microsoft.Data.Sqlite;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Data
{
    public class MessageRepository
    {
        public List<AiMessage> GetByConversation(int conversationId, int limit = 200)
        {
            var list = new List<AiMessage>();
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT Id, ConversationId, Role, Content, CreatedAt, TokensUsed, ModelUsed, IsError
                FROM Messages
                WHERE ConversationId = @cid
                ORDER BY CreatedAt ASC
                LIMIT @limit
                """;
            cmd.Parameters.AddWithValue("@cid",   conversationId);
            cmd.Parameters.AddWithValue("@limit", limit);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Read(r));
            return list;
        }

        public AiMessage Save(AiMessage msg)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Messages (ConversationId, Role, Content, CreatedAt, TokensUsed, ModelUsed, IsError)
                VALUES (@cid, @role, @content, @ca, @tokens, @model, @err);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("@cid",    msg.ConversationId);
            cmd.Parameters.AddWithValue("@role",   msg.Role.ToString());
            cmd.Parameters.AddWithValue("@content",msg.Content);
            cmd.Parameters.AddWithValue("@ca",     msg.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@tokens", msg.TokensUsed);
            cmd.Parameters.AddWithValue("@model",  (object?)msg.ModelUsed ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@err",    msg.IsError ? 1 : 0);
            msg.Id = (int)(long)(cmd.ExecuteScalar() ?? 0);
            return msg;
        }

        public void DeleteByConversation(int conversationId)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Messages WHERE ConversationId=@cid";
            cmd.Parameters.AddWithValue("@cid", conversationId);
            cmd.ExecuteNonQuery();
        }

        private static AiMessage Read(SqliteDataReader r) => new()
        {
            Id             = r.GetInt32(0),
            ConversationId = r.GetInt32(1),
            Role           = Enum.Parse<MessageRole>(r.GetString(2)),
            Content        = r.GetString(3),
            CreatedAt      = DateTime.Parse(r.GetString(4)),
            TokensUsed     = r.GetInt32(5),
            ModelUsed      = r.IsDBNull(6) ? null : r.GetString(6),
            IsError        = r.GetInt32(7) == 1,
        };
    }
}
