using System;
using System.Drawing;
using System.Windows.Forms;
namespace NeoActPlugin.Core
{
    public class DpsOverlayForm : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_NOCLOSE = 0x200;
                const int WS_EX_LAYERED = 0x80000;
                const int WS_EX_TRANSPARENT = 0x20;

                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_NOCLOSE;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                return cp;
            }
        }

        public void SetMousePassThrough(bool enable)
        {
            int exStyle = NativeMethods.GetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE);
            if (enable)
            {
                exStyle |= NativeMethods.WS_EX_TRANSPARENT;
            }
            else
            {
                exStyle &= ~NativeMethods.WS_EX_TRANSPARENT;
            }
            NativeMethods.SetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE, exStyle);
            this.Invalidate();
        }
    }

    public class DpsBarControl : Panel
    {
        private string playerName = "";
        private string dpsText = "";
        private double percent = 0;

        private Font nameFont = new Font("Microsoft JhengHei UI", 14, FontStyle.Bold);
        private Font dpsFont = new Font("Arial", 12, FontStyle.Bold);

        public DpsBarControl()
        {
            this.DoubleBuffered = true; // 減少閃爍
        }

        public void SetValues(string name, double dps, double percent)
        {
            this.playerName = name;
            this.dpsText = $"{dps:N0}/s";
            this.percent = percent;
            this.Invalidate(); // 要求重繪
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int padding = 5;
            int barWidth = this.Width - padding * 2;
            int barHeight = this.Height - padding * 2;
            Rectangle fullBarRect = new Rectangle(padding, padding, barWidth, barHeight);

            // 畫卡片背景（稍微透明的深灰）
            using (Brush cardBackground = new SolidBrush(Color.FromArgb(200, 40, 40, 40)))
            {
                var radius = 8; // 圓角半徑
                using (var path = RoundedRect(fullBarRect, radius))
                {
                    e.Graphics.FillPath(cardBackground, path);
                }
            }

            // 畫填滿條（單色 #CF721A）
            int fillWidth = Math.Max(1, (int)(barWidth * percent));
            Rectangle fillRect = new Rectangle(padding, padding, fillWidth, barHeight);

            using (Brush fillBrush = new SolidBrush(Color.FromArgb(0xCF, 0x72, 0x1A)))
            {
                var radius = 8;
                Rectangle fillRoundedRect = new Rectangle(fillRect.X, fillRect.Y, fillRect.Width, fillRect.Height);
                using (var path = RoundedRect(fillRoundedRect, radius))
                {
                    e.Graphics.FillPath(fillBrush, path);
                }
            }

            // 畫名字（靠左）
            var nameRect = new Rectangle(padding + 8, padding, barWidth / 2, barHeight);
            TextRenderer.DrawText(e.Graphics, playerName, nameFont, nameRect, Color.White,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // 畫DPS數字（靠右）
            var dpsRect = new Rectangle(padding, padding, barWidth - 8, barHeight);
            TextRenderer.DrawText(e.Graphics, dpsText, dpsFont, dpsRect, Color.White,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }
        private System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
