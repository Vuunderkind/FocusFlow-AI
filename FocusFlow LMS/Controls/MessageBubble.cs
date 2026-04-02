using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Controls
{
    public class MessageBubble : UserControl
    {
        private readonly AiMessage _msg;
        private readonly string  _agentName;

        private static readonly int PaddingH  = 20;
        private static readonly int PaddingV  = 14;
        private static readonly int MaxWidth  = 680;
        private static readonly int BubbleR   = 16;
        private static readonly int AvatarSize = 32;

        public MessageBubble(AiMessage msg, string agentName = "FocusFlow AI")
        {
            _msg       = msg;
            _agentName = agentName;
            DoubleBuffered = true;
            Dock       = DockStyle.Top;
            BackColor  = Color.Transparent;
            Cursor     = Cursors.Default;
            RecalcHeight();
        }

        private void RecalcHeight()
        {
            var isUser   = _msg.Role == MessageRole.User;
            var maxBub   = Math.Min(MaxWidth, 600);
            var textArea = maxBub - PaddingH * 2;

            using var g = Graphics.FromHwnd(IntPtr.Zero);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var font    = _msg.Role == MessageRole.User ? Theme.FontBody : Theme.FontBody;
            var size    = g.MeasureString(_msg.Content, font, textArea,
                new StringFormat { Trimming = StringTrimming.Word });
            int h = (int)size.Height + PaddingV * 2 + AvatarSize + 24;
            Height = Math.Max(h, 80);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g   = e.Graphics;
            g.SmoothingMode         = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint     = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            bool isUser = _msg.Role == MessageRole.User;
            int  cw     = ClientSize.Width;
            int  maxBub = Math.Min(MaxWidth, cw - 80);

            // ── Measure text ────────────────────────────────────────
            int textW = maxBub - PaddingH * 2;
            var sf    = new StringFormat { Trimming = StringTrimming.Word, FormatFlags = StringFormatFlags.LineLimit };
            var tsz   = g.MeasureString(_msg.Content, Theme.FontBody, textW, sf);
            int bh    = (int)tsz.Height + PaddingV * 2;
            int bw    = Math.Max((int)tsz.Width + PaddingH * 2, 80);
            bw        = Math.Min(bw, maxBub);

            // ── Layout ──────────────────────────────────────────────
            int topY    = 12;
            int avatarY = topY;
            int bubbleY = topY + AvatarSize + 8;

            int avatarX, bubbleX;
            if (isUser)
            {
                avatarX = cw - AvatarSize - 16;
                bubbleX = cw - bw - 16;
            }
            else
            {
                avatarX = 16;
                bubbleX = 16 + AvatarSize + 8;
            }

            // ── Avatar ──────────────────────────────────────────────
            var avatarRect = new Rectangle(avatarX, avatarY, AvatarSize, AvatarSize);
            if (isUser)
            {
                Theme.DrawGradientRect(g, avatarRect, AvatarSize / 2,
                    Theme.Primary, Theme.PrimaryLight);
                var usf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("U", Theme.FontBodySm, Brushes.White, avatarRect, usf);
            }
            else
            {
                var aColor = _msg.IsError ? Theme.Error : Theme.Secondary;
                Theme.DrawRoundedRect(g, avatarRect, AvatarSize / 2, Theme.WithAlpha(aColor, 30),
                    Theme.WithAlpha(aColor, 80), 1);
                var usf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("✨", new Font("Segoe UI Emoji", 14f), Brushes.White, avatarRect, usf);
            }

            // ── Name + time ─────────────────────────────────────────
            string nameText = isUser ? "Вы" : _agentName;
            string timeText = _msg.CreatedAt.ToString("HH:mm");
            var namePt = isUser
                ? new PointF(bubbleX, avatarY + 4)
                : new PointF(avatarX + AvatarSize + 8, avatarY + 4);

            g.DrawString(nameText, Theme.FontBodySm, new SolidBrush(Theme.TextSecondary), namePt);
            float nameW = g.MeasureString(nameText, Theme.FontBodySm).Width;
            g.DrawString(timeText, Theme.FontSmall, new SolidBrush(Theme.TextMuted),
                new PointF(namePt.X + nameW + 6, namePt.Y + 1));

            // ── Bubble ──────────────────────────────────────────────
            var bubbleRect = new Rectangle(bubbleX, bubbleY, bw, bh);

            if (isUser)
            {
                Theme.DrawGradientRect(g, bubbleRect, BubbleR,
                    Theme.Primary, Theme.PrimaryDark, false);
            }
            else if (_msg.IsError)
            {
                Theme.DrawRoundedRect(g, bubbleRect, BubbleR,
                    Theme.WithAlpha(Theme.Error, 25),
                    Theme.WithAlpha(Theme.Error, 60), 1);
            }
            else
            {
                Theme.DrawRoundedRect(g, bubbleRect, BubbleR, Theme.CardBg,
                    Theme.Border, 1);
            }

            // ── Text ────────────────────────────────────────────────
            var textColor = isUser ? Color.White : (_msg.IsError ? Theme.Error : Theme.TextPrimary);
            var textRect  = new Rectangle(
                bubbleRect.X + PaddingH,
                bubbleRect.Y + PaddingV,
                bw - PaddingH * 2,
                bh - PaddingV * 2);
            g.DrawString(_msg.Content, Theme.FontBody, new SolidBrush(textColor), textRect,
                new StringFormat { Trimming = StringTrimming.Word });

            // ── Tokens hint ─────────────────────────────────────────
            if (!isUser && _msg.TokensUsed > 0)
            {
                var hint = $"~{_msg.TokensUsed} токенов";
                g.DrawString(hint, Theme.FontSmall, new SolidBrush(Theme.TextMuted),
                    new PointF(bubbleRect.X, bubbleRect.Bottom + 4));
            }

            // Update height if needed
            int needed = bubbleRect.Bottom + 20;
            if (Height != needed) Height = needed;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RecalcHeight();
            Invalidate();
        }
    }
}
