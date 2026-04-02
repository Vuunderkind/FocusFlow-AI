using Microsoft.Data.Sqlite;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Data
{
    public class ConversationRepository
    {
        public List<Conversation> GetAll()
        {
            var list = new List<Conversation>();
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT c.Id, c.Title, c.AgentId, c.Model, c.IsPinned, c.CreatedAt, c.UpdatedAt,
                       (SELECT Content FROM Messages WHERE ConversationId = c.Id
                        ORDER BY CreatedAt DESC LIMIT 1) AS LastMsg,
                       (SELECT COUNT(*) FROM Messages WHERE ConversationId = c.Id) AS MsgCount
                FROM Conversations c
                ORDER BY c.IsPinned DESC, c.UpdatedAt DESC
                """;
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(Read(r));
            return list;
        }

        public Conversation? GetById(int id)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, AgentId, Model, IsPinned, CreatedAt, UpdatedAt, NULL, 0 FROM Conversations WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? Read(r) : null;
        }

        public Conversation Create(string agentId = "default", string model = "claude-opus-4-6")
        {
            var c = new Conversation
            {
                AgentId   = agentId,
                Model     = model,
                Title     = "Новый чат",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Conversations (Title, AgentId, Model, IsPinned, CreatedAt, UpdatedAt)
                VALUES (@title, @agent, @model, 0, @ca, @ua);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("@title", c.Title);
            cmd.Parameters.AddWithValue("@agent", c.AgentId);
            cmd.Parameters.AddWithValue("@model", c.Model);
            cmd.Parameters.AddWithValue("@ca", c.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@ua", c.UpdatedAt.ToString("o"));
            c.Id = (int)(long)(cmd.ExecuteScalar() ?? 0);
            return c;
        }

        public void UpdateTitle(int id, string title)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Conversations SET Title=@t, UpdatedAt=@u WHERE Id=@id";
            cmd.Parameters.AddWithValue("@t",  title);
            cmd.Parameters.AddWithValue("@u",  DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void TogglePin(int id)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Conversations SET IsPinned = 1 - IsPinned WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Conversations WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void TouchUpdatedAt(int id)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Conversations SET UpdatedAt=@u WHERE Id=@id";
            cmd.Parameters.AddWithValue("@u",  DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static Conversation Read(SqliteDataReader r) => new()
        {
            Id           = r.GetInt32(0),
            Title        = r.GetString(1),
            AgentId      = r.GetString(2),
            Model        = r.GetString(3),
            IsPinned     = r.GetInt32(4) == 1,
            CreatedAt    = DateTime.Parse(r.GetString(5)),
            UpdatedAt    = DateTime.Parse(r.GetString(6)),
            LastMessage  = r.IsDBNull(7) ? null : r.GetString(7),
            MessageCount = r.IsDBNull(8) ? 0    : r.GetInt32(8),
        };
    }
}
