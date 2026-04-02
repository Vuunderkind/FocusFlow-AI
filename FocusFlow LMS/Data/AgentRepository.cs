using Microsoft.Data.Sqlite;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Data
{
    public class AgentRepository
    {
        public List<AIAgent> GetAll()
        {
            var list = new List<AIAgent>();
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Agents ORDER BY IsBuiltIn DESC, Name ASC";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Read(r));
            return list;
        }

        public AIAgent? GetById(string id)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Agents WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? Read(r) : null;
        }

        public void Save(AIAgent a)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT OR REPLACE INTO Agents
                    (Id, Name, Description, SystemPrompt, Emoji, ColorHex, Model, Temperature, MaxTokens, IsBuiltIn, CreatedAt)
                VALUES
                    (@id, @name, @desc, @prompt, @emoji, @color, @model, @temp, @max, @bi, @ca)
                """;
            cmd.Parameters.AddWithValue("@id",     a.Id);
            cmd.Parameters.AddWithValue("@name",   a.Name);
            cmd.Parameters.AddWithValue("@desc",   a.Description);
            cmd.Parameters.AddWithValue("@prompt", a.SystemPrompt);
            cmd.Parameters.AddWithValue("@emoji",  a.Emoji);
            cmd.Parameters.AddWithValue("@color",  a.ColorHex);
            cmd.Parameters.AddWithValue("@model",  a.Model);
            cmd.Parameters.AddWithValue("@temp",   a.Temperature);
            cmd.Parameters.AddWithValue("@max",    a.MaxTokens);
            cmd.Parameters.AddWithValue("@bi",     a.IsBuiltIn ? 1 : 0);
            cmd.Parameters.AddWithValue("@ca",     a.CreatedAt.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public void Delete(string id)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Agents WHERE Id=@id AND IsBuiltIn=0";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static AIAgent Read(SqliteDataReader r) => new()
        {
            Id           = r.GetString(0),
            Name         = r.GetString(1),
            Description  = r.GetString(2),
            SystemPrompt = r.GetString(3),
            Emoji        = r.GetString(4),
            ColorHex     = r.GetString(5),
            Model        = r.GetString(6),
            Temperature  = (float)r.GetDouble(7),
            MaxTokens    = r.GetInt32(8),
            IsBuiltIn    = r.GetInt32(9) == 1,
            CreatedAt    = DateTime.Parse(r.GetString(10)),
        };
    }
}
