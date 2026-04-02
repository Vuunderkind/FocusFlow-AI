using Microsoft.Data.Sqlite;

namespace FocusFlow_LMS.Data
{
    public static class DatabaseManager
    {
        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FocusFlowAI", "focusflow.db");

        public static string ConnectionString => $"Data Source={DbPath}";

        public static void Initialize()
        {
            var dir = Path.GetDirectoryName(DbPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();

            // ── Conversations ──────────────────────────────────────────
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Conversations (
                    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title      TEXT    NOT NULL DEFAULT 'Новый чат',
                    AgentId    TEXT    NOT NULL DEFAULT 'default',
                    Model      TEXT    NOT NULL DEFAULT 'claude-opus-4-6',
                    IsPinned   INTEGER NOT NULL DEFAULT 0,
                    CreatedAt  TEXT    NOT NULL,
                    UpdatedAt  TEXT    NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();

            // ── Messages ───────────────────────────────────────────────
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Messages (
                    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                    ConversationId INTEGER NOT NULL,
                    Role           TEXT    NOT NULL,
                    Content        TEXT    NOT NULL DEFAULT '',
                    CreatedAt      TEXT    NOT NULL,
                    TokensUsed     INTEGER NOT NULL DEFAULT 0,
                    ModelUsed      TEXT,
                    IsError        INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE CASCADE
                );
                """;
            cmd.ExecuteNonQuery();

            // ── Agents ─────────────────────────────────────────────────
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Agents (
                    Id           TEXT    PRIMARY KEY,
                    Name         TEXT    NOT NULL,
                    Description  TEXT    NOT NULL DEFAULT '',
                    SystemPrompt TEXT    NOT NULL DEFAULT '',
                    Emoji        TEXT    NOT NULL DEFAULT '🤖',
                    ColorHex     TEXT    NOT NULL DEFAULT '#7C5CFC',
                    Model        TEXT    NOT NULL DEFAULT 'claude-opus-4-6',
                    Temperature  REAL    NOT NULL DEFAULT 0.7,
                    MaxTokens    INTEGER NOT NULL DEFAULT 4096,
                    IsBuiltIn    INTEGER NOT NULL DEFAULT 0,
                    CreatedAt    TEXT    NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();

            // ── Workflows ──────────────────────────────────────────────
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS Workflows (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name        TEXT    NOT NULL,
                    Description TEXT    NOT NULL DEFAULT '',
                    Emoji       TEXT    NOT NULL DEFAULT '⚡',
                    IsActive    INTEGER NOT NULL DEFAULT 1,
                    CreatedAt   TEXT    NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();

            // ── WorkflowSteps ──────────────────────────────────────────
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS WorkflowSteps (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    WorkflowId  INTEGER NOT NULL,
                    StepOrder   INTEGER NOT NULL DEFAULT 0,
                    AgentId     TEXT    NOT NULL DEFAULT 'default',
                    StepName    TEXT    NOT NULL DEFAULT '',
                    Instruction TEXT    NOT NULL DEFAULT '',
                    FOREIGN KEY (WorkflowId) REFERENCES Workflows(Id) ON DELETE CASCADE
                );
                """;
            cmd.ExecuteNonQuery();

            // ── Seed built-in agents ───────────────────────────────────
            SeedBuiltInAgents(conn);
        }

        private static void SeedBuiltInAgents(SqliteConnection conn)
        {
            var check = conn.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM Agents WHERE IsBuiltIn = 1";
            var count = (long)(check.ExecuteScalar() ?? 0L);
            if (count > 0) return;

            var agents = new[]
            {
                ("default",   "FocusFlow AI",     "Универсальный AI-ассистент",
                 "Ты — FocusFlow AI, умный и дружелюбный ассистент. Отвечай чётко, структурированно и полезно. Используй Markdown для форматирования.",
                 "✨", "#7C5CFC"),

                ("code",      "Code Master",      "Эксперт по программированию",
                 "Ты — опытный senior-разработчик с глубокими знаниями C#, Python, JavaScript, TypeScript, Go, Rust. Пиши чистый, документированный код. Объясняй решения. Используй code-блоки в Markdown.",
                 "💻", "#5C9EFF"),

                ("writer",    "Content Writer",   "Создание контента и копирайтинг",
                 "Ты — профессиональный копирайтер и контент-стратег. Создаёшь убедительные тексты: статьи, посты, рекламу, email-рассылки. Пиши живо, вовлекающе, с чёткой структурой.",
                 "✍️", "#FF7EB3"),

                ("analyst",   "Data Analyst",     "Анализ данных и бизнес-аналитика",
                 "Ты — бизнес-аналитик и data scientist. Анализируй данные, выявляй паттерны, создавай инсайты. Предлагай конкретные действия на основе данных. Используй таблицы и структурированные ответы.",
                 "📊", "#50C878"),

                ("marketing", "Marketing Expert", "Маркетинг, продвижение и стратегия",
                 "Ты — маркетолог мирового уровня с опытом в digital-маркетинге, SEO, SMM, performance-маркетинге. Создаёшь маркетинговые стратегии, воронки продаж, рекламные кампании.",
                 "📣", "#FFB344"),

                ("research",  "Research Pro",     "Глубокое исследование и анализ",
                 "Ты — профессиональный исследователь. Проводи детальный анализ темы, собирай факты, структурируй информацию. Давай развёрнутые, хорошо обоснованные ответы с примерами и источниками.",
                 "🔬", "#9B7FFF"),

                ("design",    "Design Advisor",   "UI/UX дизайн и продуктовое мышление",
                 "Ты — senior UI/UX дизайнер и продуктовый мыслитель. Консультируй по дизайну интерфейсов, пользовательскому опыту, визуальной иерархии, цветовым схемам и прототипированию.",
                 "🎨", "#FF6B9D"),

                ("teacher",   "AI Teacher",       "Объяснение сложных тем простым языком",
                 "Ты — блестящий педагог. Объясняешь любые сложные темы просто и понятно, используя аналогии, примеры и пошаговые объяснения. Адаптируешься под уровень знаний собеседника.",
                 "🎓", "#44C8F5"),
            };

            var now = DateTime.Now.ToString("o");
            foreach (var (id, name, desc, prompt, emoji, color) in agents)
            {
                var ins = conn.CreateCommand();
                ins.CommandText = """
                    INSERT OR IGNORE INTO Agents
                        (Id, Name, Description, SystemPrompt, Emoji, ColorHex, Model, Temperature, MaxTokens, IsBuiltIn, CreatedAt)
                    VALUES
                        (@id, @name, @desc, @prompt, @emoji, @color, 'claude-opus-4-6', 0.7, 4096, 1, @now)
                    """;
                ins.Parameters.AddWithValue("@id",     id);
                ins.Parameters.AddWithValue("@name",   name);
                ins.Parameters.AddWithValue("@desc",   desc);
                ins.Parameters.AddWithValue("@prompt", prompt);
                ins.Parameters.AddWithValue("@emoji",  emoji);
                ins.Parameters.AddWithValue("@color",  color);
                ins.Parameters.AddWithValue("@now",    now);
                ins.ExecuteNonQuery();
            }
        }
    }
}
