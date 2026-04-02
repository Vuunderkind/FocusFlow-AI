using FocusFlow_LMS.Controls;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Forms
{
    public class SettingsPage : UserControl
    {
        private readonly MainForm _main;

        // API keys
        private TextBox _txtAnthropic = null!;
        private TextBox _txtOpenAI    = null!;
        private TextBox _txtGemini    = null!;
        private TextBox _txtMistral   = null!;
        private TextBox _txtGroq      = null!;

        // Options
        private ComboBox      _cmbDefaultMode = null!;
        private CheckBox      _chkAutoTitle   = null!;
        private CheckBox      _chkRouterInfo  = null!;
        private TrackBar      _trkTemp        = null!;
        private Label         _lblTemp        = null!;
        private NumericUpDown _numMax         = null!;
        private Label         _lblSaved       = null!;

        public SettingsPage(MainForm main)
        {
            _main          = main;
            DoubleBuffered = true;
            BackColor      = Theme.Background;
            BuildUI();
            LoadSettings();
        }

        // ── UI ────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Top header bar
            var topBar = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Theme.SidebarBg };
            topBar.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
            };
            topBar.Controls.Add(new Label
            {
                Text = "⚙  Настройки", Font = Theme.FontH2,
                ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(24, 12),
            });
            topBar.Controls.Add(new Label
            {
                Text = "API ключи, оркестрация и параметры", Font = Theme.FontBodySm,
                ForeColor = Theme.TextSecondary, AutoSize = true, Location = new Point(24, 42),
            });

            // Scrollable content area
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

            // All sections stacked top-to-bottom using DockStyle.Top (added in REVERSE order)
            var aboutSection  = BuildAboutSection();
            var generalSection= BuildGeneralSection();
            var orchSection   = BuildOrchestrationSection();
            var apiSection    = BuildApiSection();
            var saveBar       = BuildSaveBar();

            // Add in reverse order so they appear top-to-bottom
            scroll.Controls.Add(aboutSection);
            scroll.Controls.Add(generalSection);
            scroll.Controls.Add(orchSection);
            scroll.Controls.Add(apiSection);
            scroll.Controls.Add(saveBar);

            Controls.Add(scroll);
            Controls.Add(topBar);
        }

        // ── Save bar ──────────────────────────────────────────────
        private Panel BuildSaveBar()
        {
            var bar = new Panel
            {
                Dock = DockStyle.Top, Height = 60,
                BackColor = Color.Transparent, Padding = new Padding(24, 10, 24, 0),
            };
            var btnSave = new FlatButton
            {
                Text = "💾  Сохранить настройки",
                BackColor = Theme.Primary, ForeColor = Color.White,
                UseGradient = true, GradientEnd = Theme.PrimaryLight,
                Size = new Size(220, 40), Location = new Point(0, 10),
            };
            _lblSaved = new Label
            {
                Text = "", Font = Theme.FontBodySm, ForeColor = Theme.Success,
                AutoSize = true, Location = new Point(232, 20),
            };
            btnSave.Click += (_, _) => SaveSettings();
            bar.Controls.Add(_lblSaved);
            bar.Controls.Add(btnSave);
            return bar;
        }

        // ── API Keys section ─────────────────────────────────────
        private Panel BuildApiSection()
        {
            var section = new Panel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent, Padding = new Padding(24, 8, 24, 16),
            };

            section.Controls.Add(MakeSectionLabel("🔑  API Ключи провайдеров"));

            _txtAnthropic = MakeKeyBox(); _txtOpenAI = MakeKeyBox();
            _txtGemini    = MakeKeyBox(); _txtMistral= MakeKeyBox();
            _txtGroq      = MakeKeyBox();

            var rows = new[]
            {
                ("✨ Anthropic Claude", Theme.PrimaryLight,            _txtAnthropic, "console.anthropic.com"),
                ("🟢 OpenAI GPT-4o",   Color.FromArgb(80, 200, 80),  _txtOpenAI,    "platform.openai.com"),
                ("💎 Google Gemini",   Theme.Steel,                   _txtGemini,    "aistudio.google.com"),
                ("🌪 Mistral AI",      Theme.Warning,                 _txtMistral,   "console.mistral.ai"),
                ("⚡ Groq",            Theme.Info,                    _txtGroq,      "console.groq.com"),
            };

            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Top,
                AutoSize    = true,
                AutoSizeMode= AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount    = rows.Length,
                BackColor   = Color.Transparent,
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));

            for (int i = 0; i < rows.Length; i++)
            {
                var (name, color, box, url) = rows[i];
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
                grid.Controls.Add(new Label
                {
                    Text = name, Font = Theme.FontBold, ForeColor = color,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                }, 0, i);
                box.Dock = DockStyle.Fill;
                grid.Controls.Add(box, 1, i);
                grid.Controls.Add(new Label
                {
                    Text = $"➜ {url}", Font = Theme.FontSmall, ForeColor = Theme.Steel,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(6, 0, 0, 0),
                }, 2, i);
            }

            section.Controls.Add(grid);
            return section;
        }

        // ── Orchestration section ─────────────────────────────────
        private Panel BuildOrchestrationSection()
        {
            var section = new Panel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent, Padding = new Padding(24, 8, 24, 16),
            };
            section.Controls.Add(MakeSectionLabel("🧠  Оркестрация AI"));

            // Three mode cards
            var cardsHost = new Panel
            {
                Dock = DockStyle.Top, Height = 120, BackColor = Color.Transparent,
            };
            cardsHost.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int cw = cardsHost.Width - 48;
                int bw = (cw - 24) / 3;
                var cards = new[]
                {
                    ("✦ AUTO",   Theme.Primary,    "Система сама выбирает\nлучший AI по запросу"),
                    ("⚡ FUSION", Theme.Secondary,  "Два AI отвечают →\nсинтез лучшего"),
                    ("☰ ВРУЧНУЮ",Theme.Steel,       "Вы выбираете\nпровайдера и модель"),
                };
                for (int i = 0; i < 3; i++)
                {
                    var (title, color, desc) = cards[i];
                    var r = new Rectangle(i * (bw + 12), 4, bw, 110);
                    Theme.DrawRoundedRect(g, r, 12, Theme.CardBg, Theme.WithAlpha(color, 80), 1);
                    g.DrawString(title, Theme.FontBold, new SolidBrush(color), new PointF(r.X + 12, r.Y + 10));
                    g.DrawString(desc,  Theme.FontSmall, new SolidBrush(Theme.TextSecondary), new RectangleF(r.X + 12, r.Y + 34, bw - 24, 68));
                }
            };

            section.Controls.Add(cardsHost);

            var row = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            var lblMode = new Label { Text = "Режим по умолчанию:", ForeColor = Theme.TextSecondary, Font = Theme.FontBody, AutoSize = true, Location = new Point(0, 10) };
            _cmbDefaultMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.InputBg,
                ForeColor = Theme.TextPrimary, FlatStyle = FlatStyle.Flat,
                Width = 200, Location = new Point(200, 7),
            };
            _cmbDefaultMode.Items.AddRange(new object[] { "✦ AUTO", "⚡ FUSION", "☰ ВРУЧНУЮ" });
            _cmbDefaultMode.SelectedIndex = 0;
            row.Controls.AddRange(new Control[] { lblMode, _cmbDefaultMode });
            section.Controls.Add(row);

            _chkRouterInfo = new CheckBox
            {
                Text = "Показывать информацию о выборе AI модели в ответах",
                Font = Theme.FontBody, ForeColor = Theme.TextPrimary,
                BackColor = Color.Transparent, AutoSize = true,
                Dock = DockStyle.Top, Padding = new Padding(0, 4, 0, 4),
            };
            section.Controls.Add(_chkRouterInfo);
            return section;
        }

        // ── General section ───────────────────────────────────────
        private Panel BuildGeneralSection()
        {
            var section = new Panel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent, Padding = new Padding(24, 8, 24, 16),
            };
            section.Controls.Add(MakeSectionLabel("⚙️  Параметры генерации"));

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top, Height = 90,
                ColumnCount = 2, RowCount = 2,
                BackColor = Color.Transparent,
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            // Temperature row
            grid.Controls.Add(new Label
            {
                Text = "Температура:", ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            }, 0, 0);
            var tempRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _trkTemp = new TrackBar { Minimum = 0, Maximum = 20, Value = 7, TickFrequency = 5, Dock = DockStyle.Fill, BackColor = Theme.InputBg };
            _lblTemp = new Label { Text = "0.7", ForeColor = Theme.PrimaryLight, Dock = DockStyle.Right, Width = 36, TextAlign = ContentAlignment.MiddleCenter };
            _trkTemp.ValueChanged += (_, _) => _lblTemp.Text = $"{_trkTemp.Value / 10.0:F1}";
            tempRow.Controls.AddRange(new Control[] { _lblTemp, _trkTemp });
            grid.Controls.Add(tempRow, 1, 0);

            // Max tokens row
            grid.Controls.Add(new Label
            {
                Text = "Макс. токенов:", ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            }, 0, 1);
            _numMax = new NumericUpDown
            {
                BackColor = Theme.InputBg, ForeColor = Theme.TextPrimary,
                Minimum = 256, Maximum = 32000, Value = 4096, Increment = 256,
                Dock = DockStyle.Fill,
            };
            grid.Controls.Add(_numMax, 1, 1);
            section.Controls.Add(grid);

            _chkAutoTitle = new CheckBox
            {
                Text = "Автоматически генерировать названия чатов",
                Font = Theme.FontBody, ForeColor = Theme.TextPrimary,
                BackColor = Color.Transparent, AutoSize = true,
                Dock = DockStyle.Top, Padding = new Padding(0, 6, 0, 0),
            };
            section.Controls.Add(_chkAutoTitle);
            return section;
        }

        // ── About section ─────────────────────────────────────────
        private Panel BuildAboutSection()
        {
            var section = new Panel
            {
                Dock = DockStyle.Top, Height = 200,
                BackColor = Color.Transparent, Padding = new Padding(24, 8, 24, 24),
            };
            section.Controls.Add(MakeSectionLabel("ℹ️  О программе"));

            var card = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Color.Transparent };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var r = new Rectangle(0, 0, card.Width - 2, card.Height - 2);
                Theme.DrawRoundedRect(g, r, Theme.RadiusMedium, Theme.CardBg, Theme.Border, 1);

                float y = 14;
                void Line(string t, Color c, Font f) { g.DrawString(t, f, new SolidBrush(c), new PointF(16, y)); y += f.GetHeight() + 4; }
                Line("✦ FocusFlow AI  v1.0",          Theme.PrimaryLight, Theme.FontH2);
                Line("Multi-AI Orchestration Platform", Theme.TextPrimary,  Theme.FontBody);
                y += 4;
                Line("C# .NET 10 · Windows Forms · SQLite",              Theme.TextSecondary, Theme.FontBodySm);
                Line("Anthropic Claude · OpenAI GPT · Google Gemini · Mistral · Groq", Theme.TextSecondary, Theme.FontBodySm);
                Line("Хекслет Колледж — Учебная практика ПМ3 · 2025",   Theme.TextMuted, Theme.FontSmall);
            };
            section.Controls.Add(card);
            return section;
        }

        // ── Helpers ───────────────────────────────────────────────
        private static Label MakeSectionLabel(string text) => new()
        {
            Text      = text,
            Font      = Theme.FontH3,
            ForeColor = Theme.PrimaryLight,
            Dock      = DockStyle.Top,
            Height    = 36,
            BackColor = Color.Transparent,
        };

        private static TextBox MakeKeyBox() => new()
        {
            BackColor            = Theme.InputBg,
            ForeColor            = Theme.TextPrimary,
            BorderStyle          = BorderStyle.FixedSingle,
            Font                 = Theme.FontBodySm,
            UseSystemPasswordChar= true,
            Height               = 26,
            Margin               = new Padding(0, 4, 8, 4),
        };

        // ── Load / Save ───────────────────────────────────────────
        private void LoadSettings()
        {
            var c = MainForm.Config;
            _txtAnthropic.Text = c.AnthropicApiKey;
            _txtOpenAI.Text    = c.OpenAiApiKey;
            _txtGemini.Text    = c.GeminiApiKey;
            _txtMistral.Text   = c.MistralApiKey;
            _txtGroq.Text      = c.GroqApiKey;

            _cmbDefaultMode.SelectedIndex = c.DefaultOrchestration switch
            {
                OrchestrationMode.Fusion => 1,
                OrchestrationMode.Manual => 2,
                _                        => 0,
            };
            _chkAutoTitle.Checked  = c.AutoTitleChats;
            _chkRouterInfo.Checked = c.ShowRouterInfo;
            _trkTemp.Value         = (int)(c.Temperature * 10);
            _numMax.Value          = Math.Clamp(c.MaxTokens, 256, 32000);
        }

        private void SaveSettings()
        {
            var c              = MainForm.Config;
            c.AnthropicApiKey  = _txtAnthropic.Text.Trim();
            c.OpenAiApiKey     = _txtOpenAI.Text.Trim();
            c.GeminiApiKey     = _txtGemini.Text.Trim();
            c.MistralApiKey    = _txtMistral.Text.Trim();
            c.GroqApiKey       = _txtGroq.Text.Trim();
            c.DefaultOrchestration = _cmbDefaultMode.SelectedIndex switch
            {
                1 => OrchestrationMode.Fusion,
                2 => OrchestrationMode.Manual,
                _ => OrchestrationMode.Auto,
            };
            c.AutoTitleChats   = _chkAutoTitle.Checked;
            c.ShowRouterInfo   = _chkRouterInfo.Checked;
            c.Temperature      = _trkTemp.Value / 10.0f;
            c.MaxTokens        = (int)_numMax.Value;
            c.Save();
            MainForm.ReloadServices();

            _lblSaved.Text = "✓ Сохранено!";
            var t = new System.Windows.Forms.Timer { Interval = 3000 };
            t.Tick += (_, _) => { _lblSaved.Text = ""; t.Stop(); t.Dispose(); };
            t.Start();
        }
    }
}
