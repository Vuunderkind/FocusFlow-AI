namespace FocusFlow_LMS
{
    // ============================================================
    //  FocusFlow AI — Design System
    //  Palette extracted from provided color board:
    //    Primary:   #7C5CFC → #9B7FFF  (violet gradient)
    //    Secondary: #B27FE0 → #8B6CBF  (lavender)
    //    Steel:     #6B7DB8 → #8A9BD4  (blue-gray)
    //    Dark:      #2E2D36 → #1C1B22  (charcoal cards)
    //    Background:#0D0D12             (near black)
    // ============================================================
    public static class Theme
    {
        // ---------- Backgrounds ----------
        public static readonly Color Background     = Color.FromArgb(13,  13,  18);
        public static readonly Color SidebarBg      = Color.FromArgb(17,  17,  26);
        public static readonly Color CardBg         = Color.FromArgb(26,  26,  38);
        public static readonly Color ElevatedBg     = Color.FromArgb(36,  35,  50);
        public static readonly Color InputBg        = Color.FromArgb(22,  22,  32);

        // ---------- Accents ----------
        public static readonly Color Primary        = Color.FromArgb(124, 92,  252);
        public static readonly Color PrimaryLight   = Color.FromArgb(155, 127, 255);
        public static readonly Color PrimaryDark    = Color.FromArgb(96,  68,  210);
        public static readonly Color Secondary      = Color.FromArgb(178, 127, 224);
        public static readonly Color Steel          = Color.FromArgb(107, 125, 184);

        // ---------- Text ----------
        public static readonly Color TextPrimary    = Color.FromArgb(232, 232, 240);
        public static readonly Color TextSecondary  = Color.FromArgb(140, 140, 175);
        public static readonly Color TextMuted      = Color.FromArgb(80,  80,  110);
        public static readonly Color TextOnPrimary  = Color.White;

        // ---------- Borders & Dividers ----------
        public static readonly Color Border         = Color.FromArgb(45,  44,  62);
        public static readonly Color BorderLight    = Color.FromArgb(60,  58,  80);

        // ---------- Semantic ----------
        public static readonly Color Success        = Color.FromArgb(72,  199, 116);
        public static readonly Color Warning        = Color.FromArgb(255, 180, 60);
        public static readonly Color Error          = Color.FromArgb(255, 80,  80);
        public static readonly Color Info           = Color.FromArgb(90,  190, 255);

        // ---------- Gradients ----------
        public static Color[] PrimaryGradient = { Color.FromArgb(124,92,252), Color.FromArgb(155,127,255) };
        public static Color[] SecondaryGradient = { Color.FromArgb(178,127,224), Color.FromArgb(107,85,162) };

        // ---------- Typography ----------
        public static readonly Font FontHuge    = new("Segoe UI", 28f, FontStyle.Bold);
        public static readonly Font FontH1      = new("Segoe UI", 20f, FontStyle.Bold);
        public static readonly Font FontH2      = new("Segoe UI", 15f, FontStyle.Bold);
        public static readonly Font FontH3      = new("Segoe UI", 12f, FontStyle.Bold);
        public static readonly Font FontBody    = new("Segoe UI", 10f, FontStyle.Regular);
        public static readonly Font FontBodySm  = new("Segoe UI",  9f, FontStyle.Regular);
        public static readonly Font FontSmall   = new("Segoe UI",  8f, FontStyle.Regular);
        public static readonly Font FontMono    = new("Cascadia Code", 9.5f, FontStyle.Regular);
        public static readonly Font FontMonoFallback = new("Consolas", 9.5f, FontStyle.Regular);
        public static readonly Font FontBold    = new("Segoe UI", 10f, FontStyle.Bold);
        public static readonly Font FontSemiBold= new("Segoe UI Semibold", 10f, FontStyle.Regular);

        // ---------- Spacing ----------
        public const int RadiusSmall  = 8;
        public const int RadiusMedium = 12;
        public const int RadiusLarge  = 16;
        public const int RadiusXL     = 24;

        // ---------- Helpers ----------
        public static void ApplyDarkScrollBar(Control c)
        {
            // Best-effort: set background of panel/listbox
            c.BackColor = CardBg;
            c.ForeColor = TextPrimary;
        }

        public static void DrawRoundedRect(Graphics g, Rectangle rect, int radius,
                                           Color fillColor, Color? borderColor = null, int borderWidth = 1)
        {
            using var path = GetRoundedPath(rect, radius);
            using var fill = new SolidBrush(fillColor);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillPath(fill, path);
            if (borderColor.HasValue)
            {
                using var pen = new Pen(borderColor.Value, borderWidth);
                g.DrawPath(pen, path);
            }
        }

        public static void DrawGradientRect(Graphics g, Rectangle rect, int radius,
                                            Color c1, Color c2, bool vertical = true)
        {
            using var path = GetRoundedPath(rect, radius);
            var angle = vertical ? 90f : 0f;
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, c1, c2, angle);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillPath(brush, path);
        }

        public static System.Drawing.Drawing2D.GraphicsPath GetRoundedPath(Rectangle r, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color Blend(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static Color Darken(Color c, float amount = 0.15f)
            => Blend(c, Color.Black, amount);

        public static Color Lighten(Color c, float amount = 0.15f)
            => Blend(c, Color.White, amount);

        public static Color WithAlpha(Color c, int alpha)
            => Color.FromArgb(alpha, c.R, c.G, c.B);
    }
}
