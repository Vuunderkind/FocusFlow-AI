namespace FocusFlow_LMS.Controls
{
    public class RoundedPanel : Panel
    {
        private int    _radius      = Theme.RadiusMedium;
        private Color  _borderColor = Color.Transparent;
        private int    _borderWidth = 1;
        private bool   _gradient    = false;
        private Color  _gradientEnd = Color.Transparent;

        public int   CornerRadius { get => _radius;      set { _radius      = value; Invalidate(); } }
        public Color BorderColor  { get => _borderColor; set { _borderColor = value; Invalidate(); } }
        public int   BorderWidth  { get => _borderWidth; set { _borderWidth = value; Invalidate(); } }
        public bool  UseGradient  { get => _gradient;    set { _gradient    = value; Invalidate(); } }
        public Color GradientEnd  { get => _gradientEnd; set { _gradientEnd = value; Invalidate(); } }

        public RoundedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw   = true;
            BackColor      = Theme.CardBg;
            ForeColor      = Theme.TextPrimary;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g    = e.Graphics;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (_gradient && _gradientEnd != Color.Transparent)
                Theme.DrawGradientRect(g, rect, _radius, BackColor, _gradientEnd);
            else
                Theme.DrawRoundedRect(g, rect, _radius, BackColor,
                    _borderColor == Color.Transparent ? null : _borderColor,
                    _borderWidth);

            base.OnPaint(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { /* handled in OnPaint */ }
    }

    // ── Simple flat rounded button ──────────────────────────────
    public class FlatButton : Button
    {
        private int   _radius      = Theme.RadiusMedium;
        private Color _hoverBg     = Color.Empty;
        private Color _pressBg     = Color.Empty;
        private bool  _isHovered   = false;
        private bool  _isPressed   = false;
        private bool  _useGradient = false;
        private Color _gradientEnd = Color.Transparent;

        public int   CornerRadius { get => _radius;      set { _radius      = value; Invalidate(); } }
        public bool  UseGradient  { get => _useGradient; set { _useGradient = value; Invalidate(); } }
        public Color GradientEnd  { get => _gradientEnd; set { _gradientEnd = value; Invalidate(); } }

        public FlatButton()
        {
            FlatStyle      = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor         = Cursors.Hand;
            BackColor      = Theme.Primary;
            ForeColor      = Color.White;
            Font           = Theme.FontBold;
            DoubleBuffered = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        { _isHovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e)
        { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        { _isPressed = true;  Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)
        { _isPressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g    = e.Graphics;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color bg = BackColor;
            if (_isPressed)     bg = Theme.Darken(BackColor, 0.2f);
            else if (_isHovered) bg = Theme.Lighten(BackColor, 0.1f);

            if (_useGradient && _gradientEnd != Color.Transparent)
                Theme.DrawGradientRect(g, rect, _radius, bg, _gradientEnd);
            else
                Theme.DrawRoundedRect(g, rect, _radius, bg);

            var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(Text, Font, new SolidBrush(ForeColor), rect, sf);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
    }

    // ── Icon button (square) ────────────────────────────────────
    public class IconButton : Button
    {
        private bool _isHovered = false;
        private bool _isPressed = false;
        private int  _radius    = 8;

        public int CornerRadius { get => _radius; set { _radius = value; Invalidate(); } }

        public IconButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor    = Cursors.Hand;
            BackColor = Color.Transparent;
            ForeColor = Theme.TextSecondary;
            Font      = new Font("Segoe UI", 16f);
            Size      = new Size(36, 36);
            DoubleBuffered = true;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _isPressed = true;  Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)   { _isPressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g    = e.Graphics;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (_isPressed)
                Theme.DrawRoundedRect(g, rect, _radius, Theme.WithAlpha(Theme.Primary, 60));
            else if (_isHovered)
                Theme.DrawRoundedRect(g, rect, _radius, Theme.WithAlpha(Theme.PrimaryLight, 30));

            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(Text, Font, new SolidBrush(_isHovered ? Theme.TextPrimary : ForeColor), rect, sf);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
    }

    // ── Smooth scrollable panel ─────────────────────────────────
    public class SmoothPanel : Panel
    {
        public SmoothPanel()
        {
            DoubleBuffered = true;
            AutoScroll     = true;
            BackColor      = Theme.Background;
        }
    }
}
