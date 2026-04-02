namespace FocusFlow_LMS.Controls
{
    public static class InputDialog
    {
        public static string Show(string prompt, string title, string defaultValue = "")
        {
            using var frm = new Form
            {
                Text            = title,
                Size            = new Size(420, 160),
                BackColor       = Theme.CardBg,
                ForeColor       = Theme.TextPrimary,
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
            };

            var lbl = new Label
            {
                Text      = prompt,
                Font      = Theme.FontBody,
                ForeColor = Theme.TextSecondary,
                AutoSize  = true,
                Location  = new Point(16, 16),
            };

            var txt = new TextBox
            {
                Text        = defaultValue,
                BackColor   = Theme.InputBg,
                ForeColor   = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = Theme.FontBody,
                Width       = 374,
                Location    = new Point(16, 40),
            };

            var btnOk = new FlatButton
            {
                Text        = "ОК",
                BackColor   = Theme.Primary,
                ForeColor   = Color.White,
                UseGradient = true,
                GradientEnd = Theme.PrimaryLight,
                Size        = new Size(90, 34),
                Location    = new Point(194, 82),
                DialogResult= DialogResult.OK,
            };
            var btnCancel = new FlatButton
            {
                Text        = "Отмена",
                BackColor   = Theme.ElevatedBg,
                ForeColor   = Theme.TextSecondary,
                Size        = new Size(90, 34),
                Location    = new Point(298, 82),
                DialogResult= DialogResult.Cancel,
            };

            frm.AcceptButton = btnOk;
            frm.CancelButton = btnCancel;
            frm.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });

            return frm.ShowDialog() == DialogResult.OK ? txt.Text : string.Empty;
        }
    }
}
