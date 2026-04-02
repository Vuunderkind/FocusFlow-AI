using FocusFlow_LMS.Controls;
using FocusFlow_LMS.Models;
using FocusFlow_LMS.Services;

namespace FocusFlow_LMS.Forms
{
    public class ChatPage : UserControl
    {
        private readonly MainForm _main;

        // ── State ────────────────────────────────────────────────
        public  int?              CurrentConversationId { get; private set; }
        private Conversation?     _conv;
        private AIAgent?          _agent;
        private List<AiMessage>   _messages = new();
        private CancellationTokenSource? _cts;
        private bool              _isThinking = false;
        private OrchestrationMode _orchMode   = OrchestrationMode.Auto;
        private ModelInfo?        _manualModel;

        // ── Layout ───────────────────────────────────────────────
        private Panel         _topBar       = null!;
        private Label         _lblTitle     = null!;
        private Label         _lblModel     = null!;
        private Panel         _messagesArea = null!;
        private Panel         _inputArea    = null!;
        private RichTextBox   _inputBox     = null!;
        private FlatButton    _sendBtn      = null!;
        private IconButton    _stopBtn      = null!;
        private ComboBox      _agentPicker  = null!;
        private Panel         _thinkingBar  = null!;
        private Label         _thinkingLbl  = null!;
        private System.Windows.Forms.Timer _thinkTimer = null!;
        private int           _thinkDots    = 0;

        // Mode buttons
        private FlatButton _btnAuto   = null!;
        private FlatButton _btnFusion = null!;
        private FlatButton _btnManual = null!;
        private Panel      _manualRow = null!;
        private ComboBox   _providerPicker = null!;
        private ComboBox   _modelPicker    = null!;

        // Welcome
        private Panel? _welcomePanel;

        public ChatPage(MainForm main)
        {
            _main          = main;
            _orchMode      = MainForm.Config.DefaultOrchestration;
            DoubleBuffered = true;
            BackColor      = Theme.Background;
            BuildUI();
            ShowWelcome();
        }

        // ── Build UI ─────────────────────────────────────────────
        private void BuildUI()
        {
            BuildTopBar();
            BuildThinkingBar();
            BuildMessagesArea();
            BuildInputArea();
        }

        private void BuildTopBar()
        {
            _topBar = new Panel { Dock = DockStyle.Top, Height = 104, BackColor = Theme.SidebarBg };
            _topBar.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
            };

            _lblTitle = new Label { Text = "Новый чат", Font = Theme.FontH3, ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(20, 10) };
            _lblModel = new Label { Text = "AUTO режим", Font = Theme.FontSmall, ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(20, 32) };

            // Agent picker
            _agentPicker = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.InputBg, ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat, Font = Theme.FontBodySm, Width = 160,
            };
            _agentPicker.SelectedIndexChanged += (_, _) =>
            {
                if (_agentPicker.SelectedItem is AgentItem ai) _agent = ai.Agent;
            };

            // ── Mode toggle row ───────────────────────────────────
            var modePanel = new Panel { Height = 38, BackColor = Color.Transparent };

            _btnAuto   = MakeModeBtn("✦ AUTO",   "Умный выбор ИИ");
            _btnFusion = MakeModeBtn("⚡ FUSION", "Объединение 2 ИИ");
            _btnManual = MakeModeBtn("☰ ВРУЧНУЮ", "Ваш выбор");

            _btnAuto.Click   += (_, _) => SetMode(OrchestrationMode.Auto);
            _btnFusion.Click += (_, _) => SetMode(OrchestrationMode.Fusion);
            _btnManual.Click += (_, _) => SetMode(OrchestrationMode.Manual);

            modePanel.Controls.AddRange(new Control[] { _btnAuto, _btnFusion, _btnManual });

            // ── Manual provider/model row ─────────────────────────
            _manualRow = new Panel { Height = 36, BackColor = Color.Transparent, Visible = false };
            _providerPicker = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.InputBg,
                ForeColor = Theme.TextPrimary, FlatStyle = FlatStyle.Flat, Width = 140,
                Location = new Point(0, 4),
            };
            _modelPicker = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.InputBg,
                ForeColor = Theme.TextPrimary, FlatStyle = FlatStyle.Flat, Width = 200,
                Location = new Point(148, 4),
            };

            // Fill provider list
            foreach (ProviderType p in Enum.GetValues<ProviderType>())
                _providerPicker.Items.Add(p);
            _providerPicker.SelectedIndexChanged += OnProviderChanged;

            _modelPicker.SelectedIndexChanged += (_, _) =>
            {
                if (_modelPicker.SelectedItem is ModelPickerItem mi)
                {
                    _manualModel = mi.Model;
                    _lblModel.Text = $"☰ {mi.Model.DisplayName}";
                }
            };

            _manualRow.Controls.AddRange(new Control[] { _providerPicker, _modelPicker });

            var btnClear = new IconButton { Text = "🗑" };
            btnClear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClear.Click += (_, _) => ClearChat();

            _topBar.Controls.AddRange(new Control[] { _lblTitle, _lblModel, _agentPicker, modePanel, _manualRow, btnClear });

            _topBar.Resize += (_, _) =>
            {
                _agentPicker.Location   = new Point(_topBar.Width - 180, 8);
                btnClear.Location       = new Point(_topBar.Width - 44, 8);
                modePanel.Location      = new Point(16, 52);
                modePanel.Width         = _topBar.Width - 32;
                _manualRow.Location     = new Point(16, 52 + 42);
                _manualRow.Width        = _topBar.Width - 32;

                int bw = 110;
                _btnAuto.SetBounds(0, 0, bw, 32);
                _btnFusion.SetBounds(bw + 6, 0, bw, 32);
                _btnManual.SetBounds((bw + 6) * 2, 0, bw, 32);
            };

            LoadAgentPicker();
            SetMode(_orchMode, init: true);
            Controls.Add(_topBar);
        }

        private void BuildThinkingBar()
        {
            _thinkingBar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Theme.WithAlpha(Theme.Primary, 20), Visible = false };
            _thinkingLbl = new Label { Dock = DockStyle.Fill, Text = "...", Font = Theme.FontBodySm, ForeColor = Theme.PrimaryLight, TextAlign = ContentAlignment.MiddleCenter };
            _thinkingBar.Controls.Add(_thinkingLbl);
            _thinkTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _thinkTimer.Tick += (_, _) =>
            {
                _thinkDots = (_thinkDots + 1) % 4;
                var dots = new string('.', _thinkDots);
                _thinkingLbl.Text = _orchMode switch
                {
                    OrchestrationMode.Auto   => $"✦ AUTO выбирает лучший AI{dots}",
                    OrchestrationMode.Fusion => $"⚡ FUSION: два AI отвечают одновременно{dots}",
                    OrchestrationMode.Manual => $"☰ {_manualModel?.DisplayName ?? "AI"} обрабатывает{dots}",
                    _                        => $"AI думает{dots}",
                };
            };
            Controls.Add(_thinkingBar);
        }

        private void BuildMessagesArea()
        {
            _messagesArea = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, AutoScroll = true };
            Controls.Add(_messagesArea);
        }

        private void BuildInputArea()
        {
            _inputArea = new Panel { Dock = DockStyle.Bottom, Height = 110, BackColor = Theme.SidebarBg, Padding = new Padding(16, 10, 16, 10) };
            _inputArea.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, _inputArea.Width, 0);
            };

            var inputWrapper = new RoundedPanel { Dock = DockStyle.Fill, BackColor = Theme.InputBg, CornerRadius = Theme.RadiusMedium, BorderColor = Theme.Border };
            _inputBox = new RichTextBox
            {
                BackColor = Theme.InputBg, ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.None, Font = Theme.FontBody,
                Dock = DockStyle.Fill, Multiline = true, WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical, Padding = new Padding(10, 8, 10, 8),
            };
            _inputBox.KeyDown += OnInputKeyDown;
            inputWrapper.Controls.Add(_inputBox);

            _sendBtn = new FlatButton
            {
                Text = "▶", Size = new Size(44, 44), BackColor = Theme.Primary, ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f), UseGradient = true, GradientEnd = Theme.PrimaryLight, CornerRadius = 12,
            };
            _sendBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _sendBtn.Click += (_, _) => _ = SendMessageAsync();

            _stopBtn = new IconButton { Text = "⏹", Size = new Size(44, 44), Visible = false, ForeColor = Theme.Error };
            _stopBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _stopBtn.Click += (_, _) => _cts?.Cancel();

            _inputArea.Controls.Add(inputWrapper);
            _inputArea.Controls.Add(_sendBtn);
            _inputArea.Controls.Add(_stopBtn);
            _inputArea.Resize += (_, _) =>
            {
                _sendBtn.Location = new Point(_inputArea.Width - _sendBtn.Width - 16, _inputArea.Height - _sendBtn.Height - 10);
                _stopBtn.Location = _sendBtn.Location;
                inputWrapper.Padding = new Padding(0, 0, _sendBtn.Width + 8, 0);
            };
            Controls.Add(_inputArea);
        }

        // ── Mode switching ────────────────────────────────────────
        private void SetMode(OrchestrationMode mode, bool init = false)
        {
            _orchMode = mode;

            // Style mode buttons
            void Style(FlatButton b, bool active)
            {
                b.BackColor   = active ? Theme.Primary : Theme.ElevatedBg;
                b.ForeColor   = active ? Color.White   : Theme.TextSecondary;
                b.UseGradient = active;
                b.GradientEnd = active ? Theme.PrimaryLight : Theme.ElevatedBg;
            }
            Style(_btnAuto,   mode == OrchestrationMode.Auto);
            Style(_btnFusion, mode == OrchestrationMode.Fusion);
            Style(_btnManual, mode == OrchestrationMode.Manual);

            // Show/hide manual row
            bool showManual = mode == OrchestrationMode.Manual;
            if (_manualRow != null) _manualRow.Visible = showManual;
            _topBar.Height = showManual ? 140 : 104;

            // Update label
            _lblModel.Text = mode switch
            {
                OrchestrationMode.Auto   => "✦ AUTO — умный выбор AI",
                OrchestrationMode.Fusion => "⚡ FUSION — объединение лучших AI",
                OrchestrationMode.Manual => _manualModel != null ? $"☰ {_manualModel.DisplayName}" : "☰ Выберите модель →",
                _                        => "",
            };

            // Pre-select first provider in manual mode
            if (showManual && !init && _providerPicker.Items.Count > 0 && _providerPicker.SelectedIndex < 0)
                _providerPicker.SelectedIndex = 0;
        }

        private void OnProviderChanged(object? sender, EventArgs e)
        {
            if (_providerPicker.SelectedItem is not ProviderType pt) return;
            _modelPicker.Items.Clear();
            var models = ModelInfo.ByProvider(pt);
            foreach (var m in models)
                _modelPicker.Items.Add(new ModelPickerItem(m));
            if (_modelPicker.Items.Count > 0)
                _modelPicker.SelectedIndex = 0;
        }

        private class ModelPickerItem
        {
            public ModelInfo Model { get; }
            public ModelPickerItem(ModelInfo m) => Model = m;
            public override string ToString() => $"{Model.Emoji} {Model.DisplayName}";
        }

        // ── Agent picker ─────────────────────────────────────────
        private void LoadAgentPicker()
        {
            _agentPicker.Items.Clear();
            foreach (var a in MainForm.AgentRepo.GetAll())
                _agentPicker.Items.Add(new AgentItem(a));
            if (_agentPicker.Items.Count > 0) _agentPicker.SelectedIndex = 0;
        }

        private class AgentItem
        {
            public AIAgent Agent { get; }
            public AgentItem(AIAgent a) => Agent = a;
            public override string ToString() => $"{Agent.Emoji} {Agent.Name}";
        }

        // ── Mode button factory ───────────────────────────────────
        private static FlatButton MakeModeBtn(string text, string tooltip)
        {
            var b = new FlatButton
            {
                Text        = text,
                BackColor   = Theme.ElevatedBg,
                ForeColor   = Theme.TextSecondary,
                Font        = Theme.FontBodySm,
                CornerRadius= 8,
                UseGradient = false,
            };
            new ToolTip().SetToolTip(b, tooltip);
            return b;
        }

        // ── Welcome screen ────────────────────────────────────────
        private void ShowWelcome()
        {
            _messagesArea.Controls.Clear();
            _welcomePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _welcomePanel.Paint += PaintWelcome;
            _messagesArea.Controls.Add(_welcomePanel);
        }

        private void PaintWelcome(object? sender, PaintEventArgs e)
        {
            var g  = e.Graphics;
            var cw = _welcomePanel!.Width;
            var ch = _welcomePanel.Height;
            g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int cy = Math.Max(ch / 2 - 120, 20);

            // Logo
            var logoR = new Rectangle(cw / 2 - 44, cy, 88, 88);
            Theme.DrawGradientRect(g, logoR, 44, Theme.Primary, Theme.PrimaryLight);
            var lsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("✦", new Font("Segoe UI", 34f), Brushes.White, logoR, lsf);

            var csf = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString("FocusFlow AI", Theme.FontH1, new SolidBrush(Theme.TextPrimary), new RectangleF(0, cy + 100, cw, 40), csf);
            g.DrawString("Объединение AI моделей для максимального результата", Theme.FontBody,
                new SolidBrush(Theme.TextSecondary), new RectangleF(0, cy + 145, cw, 28), csf);

            // Mode badges
            var badges = new[]
            {
                ("✦ AUTO",   Theme.Primary,    "Умный выбор: система сама выбирает лучший AI"),
                ("⚡ FUSION", Theme.Secondary,  "Два AI отвечают → синтез идеального ответа"),
                ("☰ ВРУЧНУЮ",Theme.Steel,       "Вы сами выбираете провайдера и модель"),
            };

            int bw = 200, bh = 52, gap = 12;
            int totalW = badges.Length * bw + (badges.Length - 1) * gap;
            int bx = (cw - totalW) / 2;
            int by = cy + 190;

            for (int i = 0; i < badges.Length; i++)
            {
                var (label, color, hint) = badges[i];
                var br = new Rectangle(bx + i * (bw + gap), by, bw, bh);
                Theme.DrawRoundedRect(g, br, 12, Theme.WithAlpha(color, 25), Theme.WithAlpha(color, 80), 1);
                var bsf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(label, Theme.FontBold, new SolidBrush(color), new RectangleF(br.X, br.Y + 6, br.Width, 22), bsf);
                g.DrawString(hint, Theme.FontSmall, new SolidBrush(Theme.TextMuted), new RectangleF(br.X + 4, br.Y + 28, br.Width - 8, 22), bsf);
            }

            // Provider logos
            var providers = new[]
            {
                ("✨", "Claude",  Theme.PrimaryLight),
                ("🟢", "GPT-4o",  Color.FromArgb(80, 200, 80)),
                ("💎", "Gemini",  Theme.Steel),
                ("🌪", "Mistral", Theme.Warning),
                ("⚡", "Groq",   Theme.Info),
            };
            int px = cw / 2 - providers.Length * 38;
            int py = by + bh + 24;
            g.DrawString("Поддерживаемые AI провайдеры:", Theme.FontSmall,
                new SolidBrush(Theme.TextMuted), new RectangleF(0, py - 20, cw, 18), csf);
            for (int i = 0; i < providers.Length; i++)
            {
                var (emoji, name, color) = providers[i];
                var pr = new Rectangle(px + i * 76, py, 68, 44);
                Theme.DrawRoundedRect(g, pr, 10, Theme.WithAlpha(color, 20), Theme.WithAlpha(color, 50), 1);
                var psf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(emoji, new Font("Segoe UI Emoji", 16f), Brushes.White, new RectangleF(pr.X, pr.Y + 2, pr.Width, 24), psf);
                g.DrawString(name, Theme.FontSmall, new SolidBrush(Theme.TextSecondary), new RectangleF(pr.X, pr.Y + 26, pr.Width, 16), psf);
            }

            // Hint at bottom
            g.DrawString("Введите сообщение ниже и нажмите ▶ или Enter", Theme.FontSmall,
                new SolidBrush(Theme.TextMuted), new RectangleF(0, py + 60, cw, 20), csf);
        }

        // ── Conversation load/start ───────────────────────────────
        public void StartNewConversation(string agentId = "default")
        {
            _conv = null; CurrentConversationId = null; _messages.Clear();
            _agent = MainForm.AgentRepo.GetById(agentId) ?? MainForm.AgentRepo.GetAll().FirstOrDefault();
            SyncAgentPicker();
            _lblTitle.Text = "Новый чат";
            SetMode(_orchMode);
            ShowWelcome();
        }

        public void LoadConversation(int id)
        {
            _conv = MainForm.ConvRepo.GetById(id);
            CurrentConversationId = id;
            _messages = MainForm.MsgRepo.GetByConversation(id);
            _agent = MainForm.AgentRepo.GetById(_conv?.AgentId ?? "default")
                     ?? MainForm.AgentRepo.GetAll().FirstOrDefault();
            SyncAgentPicker();
            _lblTitle.Text = _conv?.Title ?? "Чат";
            RebuildMessages();
        }

        private void SyncAgentPicker()
        {
            if (_agent == null) return;
            for (int i = 0; i < _agentPicker.Items.Count; i++)
            {
                if (_agentPicker.Items[i] is AgentItem ai && ai.Agent.Id == _agent.Id)
                { _agentPicker.SelectedIndex = i; break; }
            }
        }

        private void RebuildMessages()
        {
            _messagesArea.SuspendLayout();
            _messagesArea.Controls.Clear();
            _messagesArea.Controls.Add(new Panel { Height = 16, Dock = DockStyle.Top, BackColor = Color.Transparent });
            var agentName = _agent?.Name ?? "FocusFlow AI";
            foreach (var msg in _messages.OrderByDescending(m => m.CreatedAt))
                _messagesArea.Controls.Add(new MessageBubble(msg, agentName));
            _messagesArea.ResumeLayout();
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (_messagesArea.Controls.Count > 0)
                _messagesArea.ScrollControlIntoView(_messagesArea.Controls[0]);
        }

        // ── Send message ──────────────────────────────────────────
        private async Task SendMessageAsync()
        {
            var text = _inputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text) || _isThinking) return;
            _inputBox.Clear();

            // Ensure conversation exists
            if (_conv == null)
            {
                var agId  = _agent?.Id ?? "default";
                var model = _agent?.Model ?? MainForm.Config.DefaultModel;
                _conv = MainForm.ConvRepo.Create(agId, model);
                CurrentConversationId = _conv.Id;
            }

            // Dismiss welcome
            if (_welcomePanel != null && _messagesArea.Controls.Contains(_welcomePanel))
            { _messagesArea.Controls.Clear(); _welcomePanel = null; }

            // Save user message
            var userMsg = new AiMessage
            {
                ConversationId = _conv.Id, Role = MessageRole.User,
                Content = text, CreatedAt = DateTime.Now,
            };
            MainForm.MsgRepo.Save(userMsg);
            _messages.Add(userMsg);
            AddBubble(userMsg);

            SetThinking(true);
            _cts = new CancellationTokenSource();

            try
            {
                var history = _messages
                    .Where(m => m.Id != userMsg.Id)
                    .TakeLast(MainForm.Config.MaxHistoryMessages)
                    .Select(m => new ChatMessage
                    {
                        Role    = m.Role == MessageRole.User ? "user" : "assistant",
                        Content = m.Content,
                    }).ToList();

                var agent  = _agent ?? new AIAgent { SystemPrompt = "Ты — умный AI ассистент." };

                // Report progress in thinking bar
                var progress = new Progress<string>(msg => Invoke(() => _thinkingLbl.Text = msg));

                var result = await MainForm.Orchestrator.RunAsync(
                    systemPrompt : agent.SystemPrompt,
                    history      : history,
                    userMessage  : text,
                    mode         : _orchMode,
                    manualModel  : _manualModel,
                    temperature  : agent.Temperature,
                    maxTokens    : agent.MaxTokens,
                    progress     : progress,
                    ct           : _cts.Token);

                // Build response text with optional router info
                string responseContent;
                if (result.Success)
                {
                    responseContent = result.Text;
                    if (MainForm.Config.ShowRouterInfo && !string.IsNullOrWhiteSpace(result.StatusInfo))
                        responseContent = $"*{result.StatusInfo}*\n\n{result.Text}";
                }
                else
                {
                    responseContent = $"Ошибка: {result.Error}";
                }

                var asstMsg = new AiMessage
                {
                    ConversationId = _conv.Id,
                    Role           = MessageRole.Assistant,
                    Content        = responseContent,
                    CreatedAt      = DateTime.Now,
                    TokensUsed     = result.TotalTokens,
                    ModelUsed      = result.Decision?.Primary?.DisplayName,
                    IsError        = !result.Success,
                };
                MainForm.MsgRepo.Save(asstMsg);
                _messages.Add(asstMsg);
                AddBubble(asstMsg);

                MainForm.ConvRepo.TouchUpdatedAt(_conv.Id);

                // Auto-title on first exchange
                if (_messages.Count == 2 && MainForm.Config.AutoTitleChats)
                    _ = Task.Run(async () =>
                    {
                        var title = await MainForm.AI.GenerateTitleAsync(text);
                        MainForm.ConvRepo.UpdateTitle(_conv.Id, title);
                        Invoke(() => { _lblTitle.Text = title; _conv.Title = title; _main.LoadConversationList(); });
                    });
                else
                    _main.LoadConversationList();
            }
            catch (OperationCanceledException) { AddSystemNote("Запрос отменён."); }
            finally
            {
                SetThinking(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void AddBubble(AiMessage msg)
        {
            var bubble = new MessageBubble(msg, _agent?.Name ?? "FocusFlow AI") { Dock = DockStyle.Top };
            _messagesArea.SuspendLayout();
            _messagesArea.Controls.Add(bubble);
            _messagesArea.Controls.SetChildIndex(bubble, 0);
            _messagesArea.ResumeLayout();
            ScrollToBottom();
        }

        private void AddSystemNote(string text)
        {
            var lbl = new Label
            {
                Text = text, Font = Theme.FontSmall, ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            _messagesArea.Controls.Add(lbl);
            _messagesArea.Controls.SetChildIndex(lbl, 0);
            ScrollToBottom();
        }

        private void SetThinking(bool thinking)
        {
            _isThinking          = thinking;
            _thinkingBar.Visible = thinking;
            _sendBtn.Visible     = !thinking;
            _stopBtn.Visible     = thinking;
            if (thinking) _thinkTimer.Start();
            else          _thinkTimer.Stop();
            _inputBox.Enabled = !thinking;
        }

        private void ClearChat()
        {
            if (_conv == null) return;
            if (MessageBox.Show("Очистить историю чата?", "Подтверждение",
                MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            MainForm.MsgRepo.DeleteByConversation(_conv.Id);
            _messages.Clear();
            ShowWelcome();
        }

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && !e.Shift)
            { e.SuppressKeyPress = true; _ = SendMessageAsync(); }
        }
    }
}
