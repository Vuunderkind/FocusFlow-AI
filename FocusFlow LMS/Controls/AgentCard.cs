using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Controls
{
    public class AgentCard : UserControl
    {
        public AIAgent Agent   { get; }
        public event EventHandler<AIAgent>? ChatClicked;
        public event EventHandler<AIAgent>? EditClicked;
        public event EventHandler<AIAgent>? DeleteClicked;

        private bool _isHovered = false;

        public AgentCard(AIAgent agent)
        {
            Agent          = agent;
            DoubleBuffered = true;
            Size           = new Size(220, 180);
            Cursor         = Cursors.Hand;
            BackColor      = Color.Transparent;

            // Hover
            MouseEnter += (_, _) => { _isHovered = true;  Invalidate(); };
            MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };
            MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    ChatClicked?.Invoke(this, Agent);
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g    = e.Graphics;
            g.SmoothingMode     = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(2, 2, Width - 5, Height - 5);
            var bg   = _isHovered ? Theme.ElevatedBg : Theme.CardBg;
            var bord = _isHovered ? Theme.WithAlpha(Agent.Color, 180) : Theme.Border;
            Theme.DrawRoundedRect(g, rect, Theme.RadiusLarge, bg, bord, _isHovered ? 2 : 1);

            // Gradient top strip
            var strip = new Rectangle(rect.X, rect.Y, rect.Width, 6);
            using (var path = Theme.GetRoundedPath(new Rectangle(rect.X, rect.Y, rect.Width, 50), Theme.RadiusLarge))
            {
                var gradRect = new Rectangle(rect.X, rect.Y, rect.Width, 50);
                Theme.DrawGradientRect(g, gradRect, Theme.RadiusLarge,
                    Theme.WithAlpha(Agent.Color, 80), Theme.WithAlpha(Agent.Color, 0));
            }

            // Emoji avatar
            int emojiSize = 48;
            int emojiX    = rect.X + (rect.Width - emojiSize) / 2;
            int emojiY    = rect.Y + 18;
            var emojiRect = new Rectangle(emojiX, emojiY, emojiSize, emojiSize);
            Theme.DrawRoundedRect(g, emojiRect, 14, Theme.WithAlpha(Agent.Color, 35),
                Theme.WithAlpha(Agent.Color, 70), 1);
            var esf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(Agent.Emoji, new Font("Segoe UI Emoji", 22f), Brushes.White, emojiRect, esf);

            // Name
            var nameSf = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            var nameR  = new Rectangle(rect.X + 8, emojiRect.Bottom + 10, rect.Width - 16, 22);
            g.DrawString(Agent.Name, Theme.FontH3, new SolidBrush(Theme.TextPrimary), nameR, nameSf);

            // Description
            var descR = new Rectangle(rect.X + 10, nameR.Bottom + 4, rect.Width - 20, 34);
            var descSf = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(Agent.Description, Theme.FontSmall, new SolidBrush(Theme.TextSecondary), descR, descSf);

            // "Начать чат" button at bottom
            var btnRect = new Rectangle(rect.X + 14, rect.Bottom - 38, rect.Width - 28, 28);
            var btnBg   = _isHovered ? Agent.Color : Theme.WithAlpha(Agent.Color, 50);
            Theme.DrawRoundedRect(g, btnRect, 8, btnBg);
            var bsf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            var btnFg = _isHovered ? Color.White : Theme.WithAlpha(Color.White, 180);
            g.DrawString("✦ Начать чат", Theme.FontBodySm, new SolidBrush(btnFg), btnRect, bsf);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            // check if over action buttons (optional)
        }
    }
}
