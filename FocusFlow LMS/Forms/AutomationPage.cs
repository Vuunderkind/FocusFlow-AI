using FocusFlow_LMS.Controls;
using FocusFlow_LMS.Models;
using FocusFlow_LMS.Services;

namespace FocusFlow_LMS.Forms
{
    public class AutomationPage : UserControl
    {
        private readonly MainForm      _main;
        private readonly WorkflowService _svc;

        private Panel          _topBar    = null!;
        private ListBox        _lstWorkflows = null!;
        private RichTextBox    _txtInput  = null!;
        private RichTextBox    _txtOutput = null!;
        private FlatButton     _btnRun    = null!;
        private Label          _lblStatus = null!;
        private ProgressBar    _progress  = null!;

        private List<Workflow> _workflows = new();
        private CancellationTokenSource? _cts;

        public AutomationPage(MainForm main)
        {
            _main          = main;
            _svc           = new WorkflowService(MainForm.AI, MainForm.AgentRepo);
            DoubleBuffered = true;
            BackColor      = Theme.Background;
            BuildUI();
            LoadWorkflows();
        }

        private void BuildUI()
        {
            // ── Top bar ──────────────────────────────────────────
            _topBar = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Theme.SidebarBg };
            _topBar.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
            };

            _topBar.Controls.Add(new Label
            {
                Text = "⚡  Автоматизация",
                Font = Theme.FontH2, ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(24, 12),
            });
            _topBar.Controls.Add(new Label
            {
                Text = "Цепочки AI агентов для автоматизации задач",
                Font = Theme.FontBodySm, ForeColor = Theme.TextSecondary, AutoSize = true, Location = new Point(24, 42),
            });

            var btnNew = new FlatButton
            {
                Text = "+ Новый воркфлоу",
                BackColor = Theme.Primary, ForeColor = Color.White,
                UseGradient = true, GradientEnd = Theme.PrimaryLight,
                Size = new Size(170, 38),
            };
            btnNew.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNew.Click += (_, _) => OpenEditor(null);
            _topBar.Resize += (_, _) => btnNew.Location = new Point(_topBar.Width - 190, 16);
            _topBar.Controls.Add(btnNew);

            // ── Main split layout ─────────────────────────────────
            var split = new SplitContainer
            {
                Dock          = DockStyle.Fill,
                Orientation   = Orientation.Vertical,
                SplitterWidth = 2,
                BackColor     = Theme.Border,
                Panel1MinSize = 200,
                Panel2MinSize = 300,
            };
            // SplitterDistance must be set after the control has a valid size
            split.HandleCreated += (_, _) =>
            {
                try { if (split.Width > 500) split.SplitterDistance = 280; }
                catch { /* ignore if size is still 0 */ }
            };

            // Left: workflow list
            split.Panel1.BackColor = Theme.SidebarBg;

            var lblList = new Label
            {
                Text = "Воркфлоу", Font = Theme.FontH3, ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top, Height = 32, Padding = new Padding(12, 8, 0, 0), BackColor = Color.Transparent,
            };

            _lstWorkflows = new ListBox
            {
                Dock           = DockStyle.Fill,
                BackColor      = Theme.SidebarBg,
                ForeColor      = Theme.TextPrimary,
                BorderStyle    = BorderStyle.None,
                Font           = Theme.FontBody,
                DrawMode       = DrawMode.OwnerDrawFixed,
                ItemHeight     = 52,
            };
            _lstWorkflows.DrawItem += DrawWorkflowItem;
            _lstWorkflows.SelectedIndexChanged += OnWorkflowSelected;

            var ctx = new ContextMenuStrip { BackColor = Theme.CardBg, ForeColor = Theme.TextPrimary };
            ctx.Items.Add("✏️ Редактировать").Click += (_, _) =>
            {
                if (_lstWorkflows.SelectedItem is Workflow wf) OpenEditor(wf);
            };
            ctx.Items.Add("🗑️ Удалить").Click += (_, _) =>
            {
                if (_lstWorkflows.SelectedItem is Workflow wf) DeleteWorkflow(wf);
            };
            _lstWorkflows.ContextMenuStrip = ctx;

            split.Panel1.Controls.Add(_lstWorkflows);
            split.Panel1.Controls.Add(lblList);

            // Right: run panel
            split.Panel2.BackColor = Theme.Background;
            BuildRunPanel(split.Panel2);

            Controls.Add(split);
            Controls.Add(_topBar);
        }

        private void BuildRunPanel(Panel parent)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(20) };

            var lblIn = new Label { Text = "Входные данные:", Font = Theme.FontH3, ForeColor = Theme.TextSecondary, Dock = DockStyle.Top, Height = 28 };
            _txtInput = new RichTextBox
            {
                BackColor = Theme.InputBg, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.None,
                Font = Theme.FontBody, Dock = DockStyle.Top, Height = 120,
                Padding = new Padding(8),
            };

            _btnRun = new FlatButton
            {
                Text = "▶  Запустить воркфлоу",
                BackColor = Theme.Primary, ForeColor = Color.White,
                UseGradient = true, GradientEnd = Theme.PrimaryLight,
                Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 8, 0, 8),
            };
            _btnRun.Click += (_, _) => _ = RunWorkflowAsync();

            _progress = new ProgressBar
            {
                Dock = DockStyle.Top, Height = 6,
                Style = ProgressBarStyle.Marquee, Visible = false,
                BackColor = Theme.CardBg, ForeColor = Theme.Primary,
            };

            _lblStatus = new Label
            {
                Text = "Выберите воркфлоу из списка слева и введите входные данные.",
                Font = Theme.FontBodySm, ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top, Height = 24,
            };

            var lblOut = new Label { Text = "Результат:", Font = Theme.FontH3, ForeColor = Theme.TextSecondary, Dock = DockStyle.Top, Height = 28 };
            _txtOutput = new RichTextBox
            {
                BackColor = Theme.InputBg, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.None,
                Font = Theme.FontBody, Dock = DockStyle.Fill, ReadOnly = true,
                Padding = new Padding(8),
            };

            var btnStop = new FlatButton
            {
                Text = "⏹ Остановить",
                BackColor = Theme.Error, ForeColor = Color.White,
                Size = new Size(130, 34), Visible = false, Margin = new Padding(0, 4, 0, 0),
            };
            btnStop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStop.Click += (_, _) => _cts?.Cancel();

            // Layout
            var top = new Panel { Dock = DockStyle.Top, Height = 240, BackColor = Color.Transparent };
            top.Controls.Add(_txtInput);
            top.Controls.Add(lblIn);
            top.Controls.Add(_btnRun);
            top.Controls.Add(_progress);
            top.Controls.Add(_lblStatus);
            top.Controls.Add(btnStop);

            // Steps panel (shown inside output area)
            p.Controls.Add(_txtOutput);
            p.Controls.Add(lblOut);
            p.Controls.Add(top);
            parent.Controls.Add(p);
        }

        private void DrawWorkflowItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _workflows.Count) return;
            var wf   = _workflows[e.Index];
            var g    = e.Graphics;
            var r    = e.Bounds;
            bool sel = e.State.HasFlag(DrawItemState.Selected);

            g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var bgRect = new Rectangle(r.X + 4, r.Y + 2, r.Width - 8, r.Height - 4);
            if (sel) Theme.DrawRoundedRect(g, bgRect, 8, Theme.ElevatedBg, Theme.WithAlpha(Theme.Primary, 100), 1);
            else     g.FillRectangle(new SolidBrush(Theme.SidebarBg), r);

            var esf = new StringFormat { LineAlignment = StringAlignment.Center };
            g.DrawString(wf.Emoji, new Font("Segoe UI Emoji", 18f),
                Brushes.White, new Rectangle(r.X + 14, r.Y + 6, 32, 36), esf);

            g.DrawString(wf.Name, Theme.FontBold, new SolidBrush(Theme.TextPrimary),
                new Rectangle(r.X + 54, r.Y + 8, r.Width - 62, 18));
            g.DrawString($"{wf.Steps.Count} шагов", Theme.FontSmall,
                new SolidBrush(Theme.TextMuted), new Rectangle(r.X + 54, r.Y + 28, r.Width - 62, 16));
        }

        private void OnWorkflowSelected(object? sender, EventArgs e)
        {
            if (_lstWorkflows.SelectedItem is Workflow wf)
            {
                _lblStatus.Text = $"Воркфлоу «{wf.Name}» — {wf.Steps.Count} шагов: {string.Join(" → ", wf.Steps.Select(s => s.StepName))}";
            }
        }

        private void LoadWorkflows()
        {
            _workflows = MainForm.WfRepo.GetAll();
            _lstWorkflows.Items.Clear();
            foreach (var w in _workflows) _lstWorkflows.Items.Add(w);
        }

        private async Task RunWorkflowAsync()
        {
            if (_lstWorkflows.SelectedItem is not Workflow wf)
            {
                MessageBox.Show("Выберите воркфлоу для запуска.", "Воркфлоу не выбран");
                return;
            }

            var input = _txtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Введите входные данные.", "Ввод пуст");
                return;
            }

            _btnRun.Enabled  = false;
            _progress.Visible = true;
            _txtOutput.Clear();
            _cts = new CancellationTokenSource();

            var prog = new Progress<string>(msg =>
            {
                Invoke(() => _lblStatus.Text = msg);
            });

            try
            {
                var result = await _svc.RunAsync(wf, input, prog, _cts.Token);

                if (result.Success)
                {
                    _txtOutput.ForeColor = Theme.TextPrimary;

                    // Show step-by-step results
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < result.StepResults.Count; i++)
                    {
                        var s = result.StepResults[i];
                        sb.AppendLine($"═══ Шаг {i+1}: {s.StepName} ═══");
                        sb.AppendLine(s.Output);
                        sb.AppendLine();
                    }
                    sb.AppendLine("═══ ИТОГОВЫЙ РЕЗУЛЬТАТ ═══");
                    sb.AppendLine(result.Output);
                    _txtOutput.Text = sb.ToString();
                    _lblStatus.Text = "✓ Воркфлоу выполнен успешно!";
                }
                else
                {
                    _txtOutput.ForeColor = Theme.Error;
                    _txtOutput.Text = $"Ошибка: {result.Error}";
                    _lblStatus.Text = "✗ Воркфлоу завершился с ошибкой";
                }
            }
            catch (OperationCanceledException)
            {
                _lblStatus.Text = "Воркфлоу отменён.";
            }
            finally
            {
                _btnRun.Enabled   = true;
                _progress.Visible = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void OpenEditor(Workflow? existing)
        {
            using var dlg = new WorkflowEditorDialog(existing);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
            {
                MainForm.WfRepo.Save(dlg.Result);
                LoadWorkflows();
            }
        }

        private void DeleteWorkflow(Workflow wf)
        {
            var r = MessageBox.Show($"Удалить воркфлоу «{wf.Name}»?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.Yes)
            {
                MainForm.WfRepo.Delete(wf.Id);
                LoadWorkflows();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Workflow Editor Dialog
    // ─────────────────────────────────────────────────────────────
    public class WorkflowEditorDialog : Form
    {
        public Workflow? Result { get; private set; }
        private readonly Workflow? _existing;

        private TextBox _txtName  = null!;
        private TextBox _txtDesc  = null!;
        private TextBox _txtEmoji = null!;
        private DataGridView _grid = null!;

        public WorkflowEditorDialog(Workflow? existing)
        {
            _existing = existing;
            BuildUI();
        }

        private void BuildUI()
        {
            Text            = _existing == null ? "Создать воркфлоу" : "Редактировать воркфлоу";
            Size            = new Size(640, 560);
            BackColor       = Theme.CardBg;
            ForeColor       = Theme.TextPrimary;
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            // Top fields
            var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 100, ColumnCount = 4, RowCount = 2 };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            top.Controls.Add(new Label { Text = "Название:", ForeColor = Theme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            _txtName  = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.InputBg, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
            top.Controls.Add(_txtName, 1, 0);
            top.Controls.Add(new Label { Text = "Эмодзи:", ForeColor = Theme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
            _txtEmoji = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.InputBg, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
            top.Controls.Add(_txtEmoji, 3, 0);

            top.Controls.Add(new Label { Text = "Описание:", ForeColor = Theme.TextSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            _txtDesc  = new TextBox { Dock = DockStyle.Fill, BackColor = Theme.InputBg, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
            top.Controls.Add(_txtDesc, 1, 1);
            top.SetColumnSpan(_txtDesc, 3);

            // Steps grid
            var lblSteps = new Label { Text = "Шаги воркфлоу:", Font = Theme.FontH3, ForeColor = Theme.TextSecondary, Dock = DockStyle.Top, Height = 28 };

            var agents = MainForm.AgentRepo.GetAll();
            _grid = new DataGridView
            {
                Dock           = DockStyle.Fill,
                BackgroundColor= Theme.InputBg,
                GridColor      = Theme.Border,
                ForeColor      = Theme.TextPrimary,
                BorderStyle    = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            };
            _grid.DefaultCellStyle.BackColor  = Theme.InputBg;
            _grid.DefaultCellStyle.ForeColor  = Theme.TextPrimary;
            _grid.DefaultCellStyle.SelectionBackColor = Theme.WithAlpha(Theme.Primary, 80);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.ElevatedBg;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextSecondary;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "StepName", HeaderText = "Название шага", FillWeight = 30 });
            var agentCol = new DataGridViewComboBoxColumn
            {
                Name = "AgentId", HeaderText = "Агент", FillWeight = 25,
                DataSource    = agents,
                DisplayMember = "Name",
                ValueMember   = "Id",
            };
            _grid.Columns.Add(agentCol);
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Instruction", HeaderText = "Инструкция для шага", FillWeight = 45 });

            // Fill existing steps
            if (_existing != null)
            {
                _txtName.Text  = _existing.Name;
                _txtDesc.Text  = _existing.Description;
                _txtEmoji.Text = _existing.Emoji;
                foreach (var s in _existing.Steps)
                {
                    _grid.Rows.Add(s.StepName, s.AgentId, s.Instruction);
                }
            }
            else
            {
                _txtEmoji.Text = "⚡";
            }

            // Buttons
            var btnSave   = new FlatButton { Text = "Сохранить", BackColor = Theme.Primary, ForeColor = Color.White, UseGradient = true, GradientEnd = Theme.PrimaryLight, Width = 120, Height = 38, Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            var btnCancel = new FlatButton { Text = "Отмена",    BackColor = Theme.ElevatedBg, ForeColor = Theme.TextSecondary, Width = 100, Height = 38, Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            var btnBar    = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.Transparent };
            btnSave.Location   = new Point(490, 8);
            btnCancel.Location = new Point(380, 8);
            btnBar.Controls.AddRange(new Control[] { btnSave, btnCancel });

            btnSave.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_txtName.Text)) { MessageBox.Show("Введите название."); return; }
                var wf = _existing ?? new Workflow { CreatedAt = DateTime.Now };
                wf.Name        = _txtName.Text.Trim();
                wf.Description = _txtDesc.Text.Trim();
                wf.Emoji       = string.IsNullOrWhiteSpace(_txtEmoji.Text) ? "⚡" : _txtEmoji.Text.Trim();

                wf.Steps.Clear();
                for (int i = 0; i < _grid.Rows.Count - 1; i++)
                {
                    var row = _grid.Rows[i];
                    var sn  = row.Cells["StepName"]?.Value?.ToString()    ?? string.Empty;
                    var aid = row.Cells["AgentId"]?.Value?.ToString()     ?? "default";
                    var ins = row.Cells["Instruction"]?.Value?.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(sn))
                        wf.Steps.Add(new WorkflowStep { StepName = sn, AgentId = aid, Instruction = ins });
                }
                if (wf.Steps.Count == 0) { MessageBox.Show("Добавьте хотя бы один шаг."); return; }
                Result = wf;
                DialogResult = DialogResult.OK;
            };
            btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

            mainPanel.Controls.Add(_grid);
            mainPanel.Controls.Add(lblSteps);
            mainPanel.Controls.Add(top);
            mainPanel.Controls.Add(btnBar);

            Controls.Add(mainPanel);
        }
    }
}
