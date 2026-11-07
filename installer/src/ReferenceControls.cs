using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;
namespace Marko22.PTTP {
    public sealed class RoundedButton : Control
    {
        public const int SharedCornerRadius = 9;
        private bool hovered;
        private bool pressed;
        private Color normalFill = Color.FromArgb(23, 20, 16);
        private Color hoverFill = Color.FromArgb(39, 31, 21);
        private Color pressedFill = Color.FromArgb(17, 14, 11);
        private Color disabledFill = Color.FromArgb(22, 21, 19);
        private Color normalText = Color.FromArgb(242, 238, 229);
        private Color disabledText = Color.FromArgb(125, 119, 109);
        public Color BorderColor { get; set; }

        public RoundedButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(242, 238, 229);
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
        }

        public void SetPalette(Color normalFill, Color hoverFill, Color pressedFill, Color disabledFill,
            Color normalText, Color disabledText)
        {
            this.normalFill = normalFill;
            this.hoverFill = hoverFill;
            this.pressedFill = pressedFill;
            this.disabledFill = disabledFill;
            this.normalText = normalText;
            this.disabledText = disabledText;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { Focus(); pressed = true; } Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { pressed = true; Invalidate(); e.Handled = true; } base.OnKeyDown(e); }
        protected override void OnKeyUp(KeyEventArgs e) { bool activate = pressed && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter); pressed = false; Invalidate(); if (activate) PerformClick(); e.Handled = activate; base.OnKeyUp(e); }

        public void PerformClick()
        {
            if (Enabled && Visible) OnClick(EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            int radius = SharedCornerRadius;
            Color fill = Enabled
                ? (pressed ? pressedFill : hovered ? hoverFill : normalFill)
                : disabledFill;
            Color text = Enabled ? normalText : disabledText;
            Color top = Enabled ? ControlPaint.Light(fill, pressed ? 0.04F : 0.20F) : ControlPaint.Light(fill, 0.05F);
            Color bottom = Enabled ? ControlPaint.Dark(fill, pressed ? 0.22F : 0.16F) : ControlPaint.Dark(fill, 0.05F);
            if (pressed) { Color swap = top; top = bottom; bottom = swap; }
            using (GraphicsPath path = RoundedPath(bounds, radius))
            using (var brush = new LinearGradientBrush(bounds, top, bottom, LinearGradientMode.Vertical))
            {
                e.Graphics.FillPath(brush, path);
                if (!BorderColor.IsEmpty)
                    using (var border = new Pen(BorderColor, 1F)) e.Graphics.DrawPath(border, path);
            }
            if (Enabled)
            {
                int inset = radius;
                using (var highlight = new Pen(Color.FromArgb(pressed ? 22 : 52, Color.White), 1F))
                    e.Graphics.DrawLine(highlight, bounds.Left + inset, bounds.Top + 2, bounds.Right - inset, bounds.Top + 2);
                using (var shadow = new Pen(Color.FromArgb(pressed ? 22 : 72, Color.Black), 1F))
                    e.Graphics.DrawLine(shadow, bounds.Left + inset, bounds.Bottom - 1, bounds.Right - inset, bounds.Bottom - 1);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -4, -4), text, Color.Transparent);
        }

        private static GraphicsPath RoundedPath(Rectangle rectangle, int radius)
        {
            int diameter = Math.Max(2, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public sealed class BufferedContentPanel : Panel
    {
        public BufferedContentPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            UpdateStyles();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= 0x02000000;
                return parameters;
            }
        }
    }

    public sealed class WizardProgressBar : Control
    {
        private int progressValue;
        public int Value
        {
            get { return progressValue; }
            set { progressValue = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        public WizardProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Color.FromArgb(34, 40, 45);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);
            int fill = (int)Math.Round(ClientSize.Width * progressValue / 100D);
            if (fill > 0)
                using (var brush = new SolidBrush(Color.FromArgb(211, 157, 50))) e.Graphics.FillRectangle(brush, 0, 0, fill, ClientSize.Height);
            using (var pen = new Pen(Color.FromArgb(99, 108, 115))) e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, ClientSize.Width - 1), Math.Max(0, ClientSize.Height - 1));
        }
    }
}
