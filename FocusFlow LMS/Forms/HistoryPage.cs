using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Forms
{
    public class HistoryPage : UserControl
    {
        private readonly MainForm _main;
        private DataGridView _grid = null!;
        private TextBox _search    = null!;

        public HistoryPage(MainForm main)
        {
            _main          = main;
            DoubleBuffered = true;
            BackColor      = Theme.Background;
            BuildUI();
            LoadHistory();
        }

        private void BuildUI()
        {
            var topBar = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Theme.SidebarBg };
            topBar.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
            };
            topBar.Controls.Add(new Label { Text = "🕐  История чатов", Font = Theme.FontH2, ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(24, 12) });
            topBar.Controls.Add(new Label { Text = "Все ваши разговоры с AI", Font = Theme.FontBodySm, ForeColor = Theme.TextSecondary, AutoSize = true, Location = new Point(24, 42) });

            // Search
            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.Transparent, Padding = new Padding(16, 8, 16, 4) };
            _search = new TextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = Theme.InputBg,
                ForeColor   = Theme.TextSecondary,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = Theme.FontBody,
                PlaceholderText = "  🔍 Поиск по истории...",
            };
            _search.TextChanged += (_, _) => LoadHistory(_search.Text);
            searchPanel.Controls.Add(_search);

            // Grid
            _grid = new DataGridView
            {
                Dock                 = DockStyle.Fill,
                BackgroundColor      = Theme.Background,
                GridColor            = Theme.Border,
                ForeColor            = Theme.TextPrimary,
                BorderStyle          = BorderStyle.None,
                RowHeadersVisible    = false,
                AllowUserToAddRows   = false,
                AllowUserToDeleteRows= false,
                ReadOnly             = true,
                SelectionMode        = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect          = false,
                CellBorderStyle      = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                AutoSizeColumnsMode  = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate          = { Height = 48 },
            };
            _grid.DefaultCellStyle.BackColor  = Theme.Background;
            _grid.DefaultCellStyle.ForeColor  = Theme.TextPrimary;
            _grid.DefaultCellStyle.SelectionBackColor = Theme.WithAlpha(Theme.Primary, 60);
            _grid.DefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
            _grid.DefaultCellStyle.Font  = Theme.FontBody;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.WithAlpha(Theme.CardBg, 80);
            _grid.ColumnHeadersDefaultCellStyle.BackColor   = Theme.SidebarBg;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor   = Theme.TextSecondary;
            _grid.ColumnHeadersDefaultCellStyle.Font        = Theme.FontBold;
            _grid.ColumnHeadersHeight = 40;
            _grid.EnableHeadersVisualStyles = false;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title",      HeaderText = "Название",           FillWeight = 30 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Agent",      HeaderText = "Агент",              FillWeight = 15 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Messages",   HeaderText = "Сообщений",          FillWeight = 10 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Preview",    HeaderText = "Последнее сообщение",FillWeight = 35 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UpdatedAt",  HeaderText = "Дата",               FillWeight = 10 });

            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].Tag is int convId)
                    _main.OpenConversation(convId);
            };

            // Context menu
            var ctx = new ContextMenuStrip { BackColor = Theme.CardBg, ForeColor = Theme.TextPrimary };
            ctx.Items.Add("💬 Открыть чат").Click += (_, _) =>
            {
                if (_grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].Tag is int id)
                    _main.OpenConversation(id);
            };
            ctx.Items.Add("🗑️ Удалить").Click += (_, _) =>
            {
                if (_grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].Tag is int id)
                {
                    if (MessageBox.Show("Удалить чат?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        MainForm.ConvRepo.Delete(id);
                        LoadHistory(_search.Text);
                        _main.LoadConversationList();
                    }
                }
            };
            _grid.ContextMenuStrip = ctx;

            // Stats bar
            var statsBar = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = Theme.SidebarBg };
            statsBar.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, statsBar.Width, 0);
                var convs = MainForm.ConvRepo.GetAll();
                var txt   = $"  Всего чатов: {convs.Count}   |   Нажмите дважды для открытия   |   ПКМ для действий";
                e.Graphics.DrawString(txt, Theme.FontSmall, new SolidBrush(Theme.TextMuted), new PointF(8, 10));
            };

            Controls.Add(_grid);
            Controls.Add(searchPanel);
            Controls.Add(statsBar);
            Controls.Add(topBar);
        }

        public void LoadHistory(string filter = "")
        {
            _grid.Rows.Clear();
            var convs = MainForm.ConvRepo.GetAll();
            if (!string.IsNullOrWhiteSpace(filter))
                convs = convs.Where(c =>
                    c.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (c.LastMessage?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            foreach (var c in convs)
            {
                var agent = MainForm.AgentRepo.GetById(c.AgentId);
                var row = _grid.Rows[_grid.Rows.Add(
                    c.Title,
                    $"{agent?.Emoji ?? "✨"} {agent?.Name ?? "AI"}",
                    c.MessageCount,
                    c.LastMessage?.Length > 60 ? c.LastMessage[..60] + "..." : c.LastMessage ?? "",
                    c.UpdatedAt.ToString("dd.MM.yy HH:mm"))];
                row.Tag = c.Id;
            }
        }
    }
}
