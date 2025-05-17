using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KUpdater
{
    public partial class MainForm : Form
    {
        private bool dragging = false;
        private Point dragStart;
        public MainForm()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80000; // WS_EX_LAYERED
                return cp;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyLayeredBackground();
        }

        private void ApplyLayeredBackground()
        {
            int width = this.Width;
            int height = this.Height;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                // Rahmen-Grafiken laden (ersetze Pfade ggf.)
                var topLeft = Image.FromFile("Resources/border_top_left.png");
                var topCenter = Image.FromFile("Resources/border_top_center.png");
                var topRight = Image.FromFile("Resources/border_top_right.png");
                var rightCenter = Image.FromFile("Resources/border_right_center.png");
                var bottomRight = Image.FromFile("Resources/border_bottom_right.png");
                var bottomCenter = Image.FromFile("Resources/border_bottom_center.png");
                var bottomLeft = Image.FromFile("Resources/border_bottom_left.png");
                var leftCenter = Image.FromFile("Resources/border_left_center.png");

                int topHeight = topCenter.Height;
                int bottomHeight = bottomCenter.Height;
                int sideWidth = leftCenter.Width;

                
                // Ecken zeichnen
                g.DrawImage(topLeft, 0, 0, topLeft.Width, topLeft.Height);
                g.DrawImage(topRight, width - topRight.Width, 0, topRight.Width, topRight.Height);
                g.DrawImage(bottomLeft, 0, height - bottomLeft.Height, bottomLeft.Width, bottomLeft.Height);
                g.DrawImage(bottomRight, width - bottomRight.Width +1, height - bottomRight.Height, bottomRight.Width, bottomRight.Height);

                // Kanten strecken/zeichnen
                g.DrawImage(topCenter, new Rectangle(topLeft.Width, 0, width - topLeft.Width - topRight.Width + 10, topCenter.Height));
                g.DrawImage(bottomCenter, new Rectangle(bottomLeft.Width, height - bottomCenter.Height, width - bottomLeft.Width - bottomRight.Width + 21, bottomCenter.Height));
                g.DrawImage(leftCenter, new Rectangle(0, topLeft.Height, leftCenter.Width, height - topLeft.Height - bottomLeft.Height + 5));
                g.DrawImage(rightCenter, new Rectangle(width - rightCenter.Width, topRight.Height, rightCenter.Width, height - topRight.Height - bottomRight.Height + 5));

                // Optional: Innenfläche ausfüllen
                g.FillRectangle(Brushes.Black, new Rectangle(leftCenter.Width - 5, topCenter.Height - 5, width - leftCenter.Width * 2 + 12, height - topCenter.Height - bottomCenter.Height + 9));
            }

            SetBitmap(bmp, 255);
        }


        private void SetBitmap(Bitmap bitmap, byte opacity)
        {
            IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
            IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
            IntPtr oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

            var size = new Size(bitmap.Width, bitmap.Height);
            var pointSource = new Point(0, 0);
            var topPos = new Point(this.Left, this.Top);

            NativeMethods.BLENDFUNCTION blend = new NativeMethods.BLENDFUNCTION
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = opacity,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            NativeMethods.UpdateLayeredWindow(this.Handle, screenDc, ref topPos, ref size, memDc,
                ref pointSource, 0, ref blend, NativeMethods.ULW_ALPHA);

            NativeMethods.SelectObject(memDc, oldBitmap);
            NativeMethods.DeleteObject(hBitmap);
            NativeMethods.DeleteDC(memDc);
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            var closeRect = new Rectangle(this.Width - 35, 15, 20, 20); // Position des Close-Buttons
            if (closeRect.Contains(e.Location))
            {
                this.Close();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y < 50)
            {
                dragging = true;
                dragStart = new Point(e.X, e.Y);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                Point newLocation = new Point(this.Left + e.X - dragStart.X, this.Top + e.Y - dragStart.Y);
                this.Location = newLocation;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = false;
            }
        }

    }
}
