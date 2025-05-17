using KUpdater.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace KUpdater
{
    public partial class MainForm : Form
    {
        private bool dragging = false;
        private Point dragStart;

        private bool resizing = false;
        private Point resizeStartCursor;
        private Size resizeStartSize;
        private const int resizeHitSize = 20;

        private readonly Font buttonFont = new Font("Segoe UI", 10, FontStyle.Bold);
        private readonly List<ButtonRegion> buttons;


        public MainForm()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;



            buttons = new List<ButtonRegion>();
            buttons.Add(new ButtonRegion(() => new Rectangle(this.Width - 60, 10, 25, 15),"Exit", "btn_exit", () => this.Close()));

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
            SafeRedraw();
        }



        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (resizing)
            {
                Point delta = new Point(Cursor.Position.X - resizeStartCursor.X, Cursor.Position.Y - resizeStartCursor.Y);
                int newWidth = Math.Max(300, resizeStartSize.Width + delta.X);
                int newHeight = Math.Max(200, resizeStartSize.Height + delta.Y);
                this.Size = new Size(newWidth, newHeight);
                SafeRedraw();
                return;
            }

            if (dragging)
            {
                Point newLocation = new Point(this.Left + e.X - dragStart.X, this.Top + e.Y - dragStart.Y);
                this.Location = newLocation;
                return;
            }

            this.Cursor = new Rectangle(this.Width - resizeHitSize, this.Height - resizeHitSize, resizeHitSize, resizeHitSize)
                .Contains(e.Location) ? Cursors.SizeNWSE : Cursors.Default;

            bool needsRedraw = false;
            foreach (var btn in buttons)
            {
                bool prev = btn.IsHovered;
                btn.IsHovered = btn.Bounds.Contains(e.Location);
                if (prev != btn.IsHovered) needsRedraw = true;
            }

            if (needsRedraw)
                SafeRedraw();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                foreach (var btn in buttons)
                {
                    if (btn.Bounds.Contains(e.Location))
                    {
                        btn.IsPressed = true;
                        SafeRedraw();
                        return;
                    }
                }

                var resizeRect = new Rectangle(this.Width - resizeHitSize, this.Height - resizeHitSize, resizeHitSize, resizeHitSize);
                if (resizeRect.Contains(e.Location))
                {
                    resizing = true;
                    resizeStartCursor = Cursor.Position;
                    resizeStartSize = this.Size;
                }
                else
                {
                    dragging = true;
                    dragStart = e.Location;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            resizing = false;

            foreach (var btn in buttons)
            {
                if (btn.IsPressed && btn.Bounds.Contains(e.Location))
                {
                    btn.IsPressed = false;
                    btn.OnClick?.Invoke();
                    return;
                }
                btn.IsPressed = false;
            }

            SafeRedraw();
        }



        private void SafeRedraw()
        {
            if (this.IsDisposed || !this.IsHandleCreated)
                return;

            Bitmap bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                UI.Renderer.DrawBackground(g, this.Size);

                foreach (ButtonRegion btn in buttons)
                    btn.Draw(g, buttonFont);
            }

            SetBitmap(bmp, 255);
        }

        private void SetBitmap(Bitmap bitmap, byte opacity)
        {
            IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
            IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
            IntPtr hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
            IntPtr oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

            Size size = new Size(bitmap.Width, bitmap.Height);
            Point source = new Point(0, 0);
            Point topPos = new Point(this.Left, this.Top);

            var blend = new NativeMethods.BLENDFUNCTION
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = opacity,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            NativeMethods.UpdateLayeredWindow(this.Handle, screenDc, ref topPos, ref size, memDc,
                ref source, 0, ref blend, NativeMethods.ULW_ALPHA);

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
    }
}
