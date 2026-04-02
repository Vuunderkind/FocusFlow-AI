using FocusFlow_LMS.Controls;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Forms
{
    public class AgentsPage : UserControl
    {
        private readonly MainForm _main;
        private FlowLayoutPanel  _grid     = null!;
        private Panel            _topBar   = null!;

        public AgentsPage(MainForm main)
        {
            _main          = main;
            DoubleBuffered = true;
            BackColor      = Theme.Background;
            BuildUI();
            LoadAgents();
        }

        private void BuildUI()
        {
            // ── Top bar ──────────────────────────────────────────
            _topBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = Theme.SidebarBg,
            };
            _topBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var pen = new Pen(Theme.Border, 1);
                g.DrawLine(pen, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
            };

            var lblTitle = new Label
            {
                Text      = "🤖  AI Агенты",
                Font      = Theme.FontH2,
                ForeColor = Theme.TextPrimary,
                AutoSize  = true,
                Location  = new Point(24, 14),
            };
            var lblSub = new Label
            {
                Text      = "Специализированные AI ассистенты для любых задач",
                Font      = Theme.FontBodySm,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Location  = new Point(24, 42),
            };

            var btnCreate = new FlatButton
            {
                Text        = "+ Создать агента",
                BackColor   = Theme.Primary,
                ForeColor   = Color.White,
                UseGradient = true,
                GradientEnd = Theme.PrimaryLight,
                Size        = new Size(160, 38),
            };
            btnCreate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCreate.Click += (_, _) => OpenAgentEditor(null);
            _topBar.Resize += (_, _) => btnCreate.Location = new Point(_topBar.Width - 180, 16);

            _topBar.Controls.AddRange(new Control[] { lblTitle, lblSub, btnCreate });

            // ── Grid ─────────────────────────────────────────────
            var scroll = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll= true,
                Padding   = new Padding(16),
            };

            _grid = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                BackColor     = Color.Transparent,
                Padding       = new Padding(4),
            };

            scroll.Controls.Add(_grid);
            Controls.Add(scroll);
            Controls.Add(_topBar);
        }

        private void LoadAgents()
        {
            _grid.SuspendLayout();
            _grid.Controls.Clear();
            var agents = MainForm.AgentRepo.GetAll();

            foreach (var agent in agents)
            {
                var card = new AgentCard(agent) { Margin = new Padding(8) };
                card.ChatClicked   += (_, a) => _main.StartNewChat(a.Id);
                card.EditClicked   += (_, a) => OpenAgentEditor(a);
                card.DeleteClicked += (_, a) => DeleteAgent(a);

                // Right-click menu on card
                var ctx = new ContextMenuStrip { BackColor = Theme.CardBg, ForeColor = Theme.TextPrimary };
                if (!agent.IsBuiltIn)
                {
                    ctx.Items.Add("✏️ Редактировать").Click += (_, _) => OpenAgentEditor(agent);
                    ctx.Items.Add("🗑️ Удалить").Click += (_, _) => DeleteAgent(agent);
                }
                else
                {
                    ctx.Items.Add("(встроенный агент)").Enabled = false;
                }
                card.ContextMenuStrip = ctx;

                _grid.Controls.Add(card);
            }

            _grid.ResumeLayout();
        }

        private void OpenAgentEditor(AIAgent? existing)
        {
            using var dlg = new AgentEditorDialog(existing);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                MainForm.AgentRepo.Save(dlg.Result);
                LoadAgents();
            }
        }

        private void DeleteAgent(AIAgent agent)
        {
            if (agent.IsBuiltIn)
            {
                MessageBox.Show("Встроенные агенты нельзя удалить.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var r = MessageBox.Show($"Удалить агента «{agent.Name}»?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.Yes)
            {
                MainForm.AgentRepo.Delete(agent.Id);
                LoadAgents();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Agent Editor Dialog
    // ─────────────────────────────────────────────────────────────
    public class AgentEditorDialog : Form
    {
        public AIAgent? Result { get; private set; }
        private readonly AIAgent? _existing;

        private TextBox _txtName     = null!;
        private TextBox _txtDesc     = null!;
        private TextBox _txtEmoji    = null!;
        private TextBox _txtColor    = null!;
        private ComboBox _cmbModel   = null!;
        private TrackBar _trkTemp    = null!;
        private Label    _lblTemp    = null!;

        public AgentEditorDialog(AIAgent? existing)
        {
            _existing = existing;
            BuildUI();
        }

        private void BuildUI()
        {
            Text            = _existing == null ? "Создать агента" : "Редактировать агента";
            Size            = new Size(540, 560);
            MinimumSize     = new Size(480, 520);
            BackColor       = Theme.CardBg;
            ForeColor       = Theme.TextPrimary;
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;

            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 9,
                Padding     = new Padding(20),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(layout, 0, "Имя агента:", _txtName = MakeTextBox());
            AddRow(layout, 1, "Описание:",   _txtDesc = MakeTextBox());
            AddRow(layout, 2, "Эмодзи:",     _txtEmoji = MakeTextBox(40));
            AddRow(layout, 3, "Цвет (HEX):", _txtColor = MakeTextBox(80));

            _cmbModel = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor     = Theme.InputBg,
                ForeColor     = Theme.TextPrimary,
                FlatStyle     = FlatStyle.Flat,
                Dock          = DockStyle.Fill,
            };
            _cmbModel.Items.AddRange(new object[]
            {
                "claude-opus-4-6",
                "claude-sonnet-4-6",
                "claude-haiku-4-5-20251001",
            });
            _cmbModel.SelectedIndex = 0;
            AddRow(layout, 4, "Модель:", _cmbModel);

            // Temperature
            _trkTemp = new TrackBar { Minimum = 0, Maximum = 20, Value = 7, TickFrequency = 5, Dock = DockStyle.Fill };
            _lblTemp = new Label   { Text = "0.7", AutoSize = true, ForeColor = Theme.TextSecondary };
            _trkTemp.ValueChanged += (_, _) => _lblTemp.Text = $"{_trkTemp.Value / 10.0:F1}";
            var tempPanel = new Panel { Dock = DockStyle.Fill };
            tempPanel.Controls.Add(_lblTemp);
            tempPanel.Controls.Add(_trkTemp);
            _trkTemp.Dock = DockStyle.Fill;
            _lblTemp.Dock = DockStyle.Right;
            AddRow(layout, 5, "Температура:", tempPanel);

            // System prompt
            var lblPr = new Label { Text = "Системный\nпромпт:", ForeColor = Theme.TextSecondary, AutoSize = true };
            var promptBox = new RichTextBox
            {
                BackColor   = Theme.InputBg,
                ForeColor   = Theme.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font        = Theme.FontBodySm,
                Dock        = DockStyle.Fill,
            };
            layout.Controls.Add(lblPr,      0, 6);
            layout.Controls.Add(promptBox,  1, 6);

            // Buttons
            var btnSave = new FlatButton { Text = "Сохранить", BackColor = Theme.Primary, ForeColor = Color.White, UseGradient = true, GradientEnd = Theme.PrimaryLight, Dock = DockStyle.Right, Width = 120, Height = 38 };
            var btnCancel = new FlatButton { Text = "Отмена", BackColor = Theme.ElevatedBg, ForeColor = Theme.TextSecondary, Dock = DockStyle.Left, Width = 100, Height = 38 };
            var btnRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            btnRow.Controls.Add(btnSave);
            btnRow.Controls.Add(btnCancel);
            layout.Controls.Add(new Label(), 0, 7);
            layout.Controls.Add(btnRow, 1, 7);

            btnSave.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_txtName.Text))
                { MessageBox.Show("Введите имя агента."); return; }

                Result = _existing != null ? _existing : new AIAgent { CreatedAt = DateTime.Now };
                Result.Name         = _txtName.Text.Trim();
                Result.Description  = _txtDesc.Text.Trim();
                Result.Emoji        = string.IsNullOrWhiteSpace(_txtEmoji.Text) ? "🤖" : _txtEmoji.Text.Trim();
                Result.ColorHex     = string.IsNullOrWhiteSpace(_txtColor.Text) ? "#7C5CFC" : _txtColor.Text.Trim();
                Result.Model        = _cmbModel.SelectedItem?.ToString() ?? "claude-opus-4-6";
                Result.Temperature  = _trkTemp.Value / 10.0f;
                Result.SystemPrompt = promptBox.Text.Trim();
                DialogResult = DialogResult.OK;
            };
            btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

            Controls.Add(layout);

            // Fill existing data
            if (_existing != null)
            {
                _txtName.Text   = _existing.Name;
                _txtDesc.Text   = _existing.Description;
                _txtEmoji.Text  = _existing.Emoji;
                _txtColor.Text  = _existing.ColorHex;
                promptBox.Text  = _existing.SystemPrompt;
                _trkTemp.Value  = (int)(_existing.Temperature * 10);
                var mi = _cmbModel.Items.IndexOf(_existing.Model);
                if (mi >= 0) _cmbModel.SelectedIndex = mi;
            }
        }

        private static void AddRow(TableLayoutPanel tl, int row, string label, Control ctrl)
        {
            tl.Controls.Add(new Label { Text = label, ForeColor = Theme.TextSecondary, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 8, 0, 0) }, 0, row);
            ctrl.Dock = DockStyle.Fill;
            tl.Controls.Add(ctrl, 1, row);
        }

        private static TextBox MakeTextBox(int? width = null)
        {
            var tb = new TextBox { BackColor = Theme.InputBg, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
            if (width.HasValue) tb.Width = width.Value;
            return tb;
        }
    }
}
