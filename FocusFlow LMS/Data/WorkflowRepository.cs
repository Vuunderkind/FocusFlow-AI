using Microsoft.Data.Sqlite;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Data
{
    public class WorkflowRepository
    {
        public List<Workflow> GetAll()
        {
            var list = new List<Workflow>();
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Description, Emoji, IsActive, CreatedAt FROM Workflows ORDER BY CreatedAt DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var w = ReadWorkflow(r);
                w.Steps = GetSteps(conn, w.Id);
                list.Add(w);
            }
            return list;
        }

        public Workflow Save(Workflow w)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            if (w.Id == 0)
            {
                var ins = conn.CreateCommand();
                ins.CommandText = """
                    INSERT INTO Workflows (Name, Description, Emoji, IsActive, CreatedAt)
                    VALUES (@n, @d, @e, @a, @ca);
                    SELECT last_insert_rowid();
                    """;
                ins.Parameters.AddWithValue("@n",  w.Name);
                ins.Parameters.AddWithValue("@d",  w.Description);
                ins.Parameters.AddWithValue("@e",  w.Emoji);
                ins.Parameters.AddWithValue("@a",  w.IsActive ? 1 : 0);
                ins.Parameters.AddWithValue("@ca", w.CreatedAt.ToString("o"));
                w.Id = (int)(long)(ins.ExecuteScalar() ?? 0);
            }
            else
            {
                var upd = conn.CreateCommand();
                upd.CommandText = "UPDATE Workflows SET Name=@n, Description=@d, Emoji=@e, IsActive=@a WHERE Id=@id";
                upd.Parameters.AddWithValue("@n",  w.Name);
                upd.Parameters.AddWithValue("@d",  w.Description);
                upd.Parameters.AddWithValue("@e",  w.Emoji);
                upd.Parameters.AddWithValue("@a",  w.IsActive ? 1 : 0);
                upd.Parameters.AddWithValue("@id", w.Id);
                upd.ExecuteNonQuery();

                var del = conn.CreateCommand();
                del.CommandText = "DELETE FROM WorkflowSteps WHERE WorkflowId=@id";
                del.Parameters.AddWithValue("@id", w.Id);
                del.ExecuteNonQuery();
            }

            for (int i = 0; i < w.Steps.Count; i++)
            {
                var s = w.Steps[i];
                s.WorkflowId = w.Id;
                s.StepOrder  = i;
                var si = conn.CreateCommand();
                si.CommandText = """
                    INSERT INTO WorkflowSteps (WorkflowId, StepOrder, AgentId, StepName, Instruction)
                    VALUES (@wid, @order, @agent, @name, @instr);
                    SELECT last_insert_rowid();
                    """;
                si.Parameters.AddWithValue("@wid",   s.WorkflowId);
                si.Parameters.AddWithValue("@order", s.StepOrder);
                si.Parameters.AddWithValue("@agent", s.AgentId);
                si.Parameters.AddWithValue("@name",  s.StepName);
                si.Parameters.AddWithValue("@instr", s.Instruction);
                s.Id = (int)(long)(si.ExecuteScalar() ?? 0);
            }

            tx.Commit();
            return w;
        }

        public void Delete(int id)
        {
            using var conn = new SqliteConnection(DatabaseManager.ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Workflows WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static List<WorkflowStep> GetSteps(SqliteConnection conn, int workflowId)
        {
            var steps = new List<WorkflowStep>();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, WorkflowId, StepOrder, AgentId, StepName, Instruction FROM WorkflowSteps WHERE WorkflowId=@id ORDER BY StepOrder";
            cmd.Parameters.AddWithValue("@id", workflowId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                steps.Add(new WorkflowStep
                {
                    Id          = r.GetInt32(0),
                    WorkflowId  = r.GetInt32(1),
                    StepOrder   = r.GetInt32(2),
                    AgentId     = r.GetString(3),
                    StepName    = r.GetString(4),
                    Instruction = r.GetString(5),
                });
            return steps;
        }

        private static Workflow ReadWorkflow(SqliteDataReader r) => new()
        {
            Id          = r.GetInt32(0),
            Name        = r.GetString(1),
            Description = r.GetString(2),
            Emoji       = r.GetString(3),
            IsActive    = r.GetInt32(4) == 1,
            CreatedAt   = DateTime.Parse(r.GetString(5)),
        };
    }
}
