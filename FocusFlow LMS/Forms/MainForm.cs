using FocusFlow_LMS.Controls;
using FocusFlow_LMS.Data;
using FocusFlow_LMS.Models;
using FocusFlow_LMS.Services;

namespace FocusFlow_LMS.Forms
{
    public class MainForm : Form
    {
        // ── Services & Repos ─────────────────────────────────────
        public static AppConfig              Config        = AppConfig.Load();
        public static AIService              AI            = new(Config);
        public static OrchestrationService   Orchestrator  = BuildOrchestrator(Config);
        public static ConversationRepository ConvRepo      = new();
        public static MessageRepository      MsgRepo       = new();
        public static AgentRepository        AgentRepo     = new();
        public static WorkflowRepository     WfRepo        = new();

        public static OrchestrationService BuildOrchestrator(AppConfig cfg)
        {
            var registry = new ProviderRegistry(cfg);
            var router   = new AIRouter(registry);
            return new OrchestrationService(registry, router, cfg);
        }

        public static void ReloadServices()
        {
            Config       = AppConfig.Load();
            AI           = new AIService(Config);
            Orchestrator = BuildOrchestrator(Config);
        }

        // ── Layout controls ──────────────────────────────────────
        private Panel          _sidebar    = null!;
        private Panel          _content    = null!;

        // Sidebar elements
        private Label          _logo       = null!;
        private FlatButton     _btnNewChat = null!;
        private TextBox        _searchBox  = null!;
        private Panel          _convList   = null!;
        private Panel          _sideBottom = null!;

        // Nav buttons
        private Button         _navChat    = null!;
        private Button         _navAgents  = null!;
        private Button         _navAuto    = null!;
        private Button         _navHistory = null!;
        private Button         _navSettings= null!;

        // Pages (lazy created)
        private ChatPage?      _chatPage;
        private AgentsPage?    _agentsPage;
        private AutomationPage? _autoPage;
        private HistoryPage?   _historyPage;
        private SettingsPage?  _settingsPage;

        private Control?       _currentPage;
        private Button?        _activeNav;

        public MainForm()
        {
            InitializeForm();
            BuildUI();
            LoadConversationList();

            // Show chat page by default
            NavigateTo(_chatPage ??= new ChatPage(this), _navChat);

            // First-run check — defer until after handle is created
            Load += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(Config.AnthropicApiKey))
                    ShowFirstRunPrompt();
            };
        }

        private void InitializeForm()
        {
            Text            = "FocusFlow AI";
            Size            = new Size(1280, 800);
            MinimumSize     = new Size(900, 600);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Theme.Background;
            ForeColor       = Theme.TextPrimary;
            DoubleBuffered  = true;
            Font            = Theme.FontBody;
        }

        private void BuildUI()
        {
            // ── Sidebar ─────────────────────────────────────────────
            _sidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 260,
                BackColor = Theme.SidebarBg,
            };
            _sidebar.Paint += (s, e) =>
            {
                // Right border line
                var g = e.Graphics;
                using var pen = new Pen(Theme.Border, 1);
                g.DrawLine(pen, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height);
            };

            // Logo
            _logo = new Label
            {
                Text      = "✦ FocusFlow AI",
                Font      = Theme.FontH2,
                ForeColor = Theme.PrimaryLight,
                AutoSize  = false,
                Height    = 60,
                Dock      = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0),
                BackColor = Color.Transparent,
            };

            // New Chat button
            _btnNewChat = new FlatButton
            {
                Text        = "+ Новый чат",
                Dock        = DockStyle.Top,
                Height      = 44,
                BackColor   = Theme.Primary,
                ForeColor   = Color.White,
                Font        = Theme.FontBold,
                UseGradient = true,
                GradientEnd = Theme.PrimaryLight,
                Margin      = new Padding(12, 0, 12, 8),
            };
            _btnNewChat.Click += (_, _) => StartNewChat();

            var btnWrapper = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent, Padding = new Padding(12, 8, 12, 0) };
            btnWrapper.Controls.Add(_btnNewChat);

            // Search box
            _searchBox = new TextBox
            {
                PlaceholderText = "  🔍 Поиск чатов...",
                BackColor       = Theme.InputBg,
                ForeColor       = Theme.TextSecondary,
                BorderStyle     = BorderStyle.None,
                Font            = Theme.FontBody,
                Height          = 34,
                Dock            = DockStyle.Top,
            };
            _searchBox.TextChanged += (_, _) => LoadConversationList(_searchBox.Text);
            var searchWrapper = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.Transparent, Padding = new Padding(12, 4, 12, 4) };
            searchWrapper.Controls.Add(_searchBox);

            // Conversation list
            _convList = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll= true,
            };

            // Bottom navigation
            _sideBottom = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 52,
                BackColor = Theme.SidebarBg,
            };
            _sideBottom.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, _sideBottom.Width, 0);
            };

            _navAgents   = CreateNavBtn("🤖", "Агенты");
            _navAuto     = CreateNavBtn("⚡", "Авто");
            _navHistory  = CreateNavBtn("🕐", "История");
            _navSettings = CreateNavBtn("⚙", "Настройки");

            _navAgents.Click   += (_, _) => NavigateTo(_agentsPage   ??= new AgentsPage(this), _navAgents);
            _navAuto.Click     += (_, _) => NavigateTo(_autoPage     ??= new AutomationPage(this), _navAuto);
            _navHistory.Click  += (_, _) => NavigateTo(_historyPage  ??= new HistoryPage(this), _navHistory);
            _navSettings.Click += (_, _) => NavigateTo(_settingsPage ??= new SettingsPage(this), _navSettings);

            var bottomLayout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 4,
                RowCount    = 1,
                BackColor   = Color.Transparent,
            };
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            bottomLayout.Controls.Add(_navAgents,   0, 0);
            bottomLayout.Controls.Add(_navAuto,     1, 0);
            bottomLayout.Controls.Add(_navHistory,  2, 0);
            bottomLayout.Controls.Add(_navSettings, 3, 0);
            _sideBottom.Controls.Add(bottomLayout);

            _sidebar.Controls.Add(_convList);
            _sidebar.Controls.Add(searchWrapper);
            _sidebar.Controls.Add(btnWrapper);
            _sidebar.Controls.Add(_logo);
            _sidebar.Controls.Add(_sideBottom);

            // ── Content area ────────────────────────────────────────
            _content = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.Background,
            };

            // ── Chat nav button (top sidebar) ────────────────────────
            _navChat = new Button
            {
                Visible = false,   // always active implicitly
            };

            Controls.Add(_content);
            Controls.Add(_sidebar);
        }

        private Button CreateNavBtn(string emoji, string label)
        {
            var btn = new Button
            {
                Text      = $"{emoji}\n{label}",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSecondary,
                Dock      = DockStyle.Fill,
                Font      = new Font("Segoe UI Emoji", 8f),
                Cursor    = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize     = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.WithAlpha(Theme.Primary, 30);
            btn.FlatAppearance.MouseDownBackColor = Theme.WithAlpha(Theme.Primary, 60);
            return btn;
        }

        // ── Navigation ─────────────────────────────────────────────
        public void NavigateTo(Control page, Button? navBtn)
        {
            if (_currentPage != null)
            {
                _content.Controls.Remove(_currentPage);
                _currentPage.Visible = false;
            }

            page.Dock    = DockStyle.Fill;
            page.Visible = true;
            _content.Controls.Add(page);
            _currentPage = page;

            // Highlight active nav button
            if (_activeNav != null)
            {
                _activeNav.ForeColor = Theme.TextSecondary;
                _activeNav.BackColor = Color.Transparent;
            }
            if (navBtn != null)
            {
                navBtn.ForeColor = Theme.PrimaryLight;
                _activeNav = navBtn;
            }
        }

        // ── Chat management ─────────────────────────────────────────
        public void StartNewChat(string agentId = "default")
        {
            _chatPage ??= new ChatPage(this);
            _chatPage.StartNewConversation(agentId);
            NavigateTo(_chatPage, _navChat);
            LoadConversationList();
        }

        public void OpenConversation(int conversationId)
        {
            _chatPage ??= new ChatPage(this);
            _chatPage.LoadConversation(conversationId);
            NavigateTo(_chatPage, _navChat);
        }

        public void LoadConversationList(string filter = "")
        {
            _convList.Controls.Clear();
            var convs = ConvRepo.GetAll();
            if (!string.IsNullOrWhiteSpace(filter))
                convs = convs.Where(c => c.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            // Add bottom spacer
            _convList.Controls.Add(new Panel { Height = 8, Dock = DockStyle.Top, BackColor = Color.Transparent });

            foreach (var conv in convs)
            {
                var item = CreateConvItem(conv);
                _convList.Controls.Add(item);
            }
        }

        private Panel CreateConvItem(Conversation conv)
        {
            var item = new Panel
            {
                Height    = 62,
                Dock      = DockStyle.Top,
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
                Padding   = new Padding(8, 4, 8, 4),
            };

            bool isHovered = false;
            item.Paint += (s, e) =>
            {
                var g    = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var r    = new Rectangle(4, 2, item.Width - 8, item.Height - 4);
                if (isHovered)
                    Theme.DrawRoundedRect(g, r, 10, Theme.ElevatedBg, Theme.WithAlpha(Theme.Primary, 60), 1);
                else
                    Theme.DrawRoundedRect(g, r, 10, Color.Transparent);

                // Pin indicator
                if (conv.IsPinned)
                {
                    using var pinBrush = new SolidBrush(Theme.Warning);
                    g.FillEllipse(pinBrush, r.Right - 14, r.Y + 6, 6, 6);
                }

                // Title
                var titleR = new Rectangle(r.X + 10, r.Y + 8, r.Width - 24, 20);
                var titleSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
                g.DrawString(conv.Title, Theme.FontBold, new SolidBrush(Theme.TextPrimary), titleR, titleSf);

                // Preview
                if (!string.IsNullOrEmpty(conv.LastMessage))
                {
                    var prevR = new Rectangle(r.X + 10, r.Y + 30, r.Width - 24, 18);
                    var prevSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
                    g.DrawString(conv.LastMessage, Theme.FontSmall, new SolidBrush(Theme.TextMuted), prevR, prevSf);
                }

                // Time
                var timeStr = FormatTime(conv.UpdatedAt);
                var timeSz  = g.MeasureString(timeStr, Theme.FontSmall);
                g.DrawString(timeStr, Theme.FontSmall, new SolidBrush(Theme.TextMuted),
                    r.Right - timeSz.Width - 6, r.Y + 9);
            };

            item.MouseEnter += (_, _) => { isHovered = true;  item.Invalidate(); };
            item.MouseLeave += (_, _) => { isHovered = false; item.Invalidate(); };
            item.Click      += (_, _) => OpenConversation(conv.Id);

            // Right-click context menu
            var ctx = new ContextMenuStrip { BackColor = Theme.CardBg, ForeColor = Theme.TextPrimary };
            ctx.Items.Add("📌 Закрепить / Открепить").Click += (_, _) =>
            {
                ConvRepo.TogglePin(conv.Id);
                LoadConversationList();
            };
            ctx.Items.Add("✏️ Переименовать").Click += (_, _) =>
            {
                var title = FocusFlow_LMS.Controls.InputDialog.Show("Новое название:", "Переименовать", conv.Title);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    ConvRepo.UpdateTitle(conv.Id, title);
                    LoadConversationList();
                }
            };
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add("🗑️ Удалить").Click += (_, _) =>
            {
                var r = MessageBox.Show("Удалить этот чат?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r == DialogResult.Yes)
                {
                    ConvRepo.Delete(conv.Id);
                    LoadConversationList();
                    if (_chatPage?.CurrentConversationId == conv.Id)
                        StartNewChat();
                }
            };
            item.ContextMenuStrip = ctx;
            return item;
        }

        private static string FormatTime(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1)  return "сейчас";
            if (diff.TotalHours   < 1)  return $"{(int)diff.TotalMinutes}м";
            if (diff.TotalDays    < 1)  return dt.ToString("HH:mm");
            if (diff.TotalDays    < 7)  return dt.ToString("ddd");
            return dt.ToString("dd.MM");
        }

        // ── First run ───────────────────────────────────────────────
        private void ShowFirstRunPrompt()
        {
            var result = MessageBox.Show(
                "Добро пожаловать в FocusFlow AI!\n\n" +
                "Для работы необходим API ключ Anthropic Claude.\n" +
                "Открыть настройки сейчас?",
                "Настройка FocusFlow AI",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
                NavigateTo(_settingsPage ??= new SettingsPage(this), _navSettings);
        }
    }
}
