// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using KUpdater.Scripting.Theme;
using SkiaSharp;

namespace KUpdater.UI {
    public class Renderer : IDisposable {
        private readonly Form _form;
        private readonly ITheme _theme;
        private readonly ControlManager _uiElementManager;
        private readonly System.Windows.Forms.Timer _renderTimer;
        private bool _needsRender;
        private SKBitmap? _cachedSkBitmap;
        private SKSurface? _cachedSurface;
        private Bitmap? _cachedBmp;
        private bool _disposed;
        private readonly SKPaint _fillPaint = new() { IsAntialias = true };
        public bool IsRendering { get; private set; }

        public Renderer(Form form, ControlManager uiElementManager, ITheme theme) {
            _form = form;
            _uiElementManager = uiElementManager;
            _theme = theme;

            _renderTimer = new System.Windows.Forms.Timer { Interval = 33 };
            _renderTimer.Tick += (s, e) => RenderTick();
            _renderTimer.Start();
        }

        public void RequestRender() => _needsRender = true;

        private void RenderTick() {
            if (!_needsRender || _disposed || _form.IsDisposed)
                return;

            IsRendering = true;
            try {
                _needsRender = false;
                (_theme as ThemeBase)?.ApplyLastState();
                Redraw();
            }
            finally {
                IsRendering = false;
            }
        }

        public void EnsureBuffers(int width, int height) {
            if (_cachedSkBitmap == null || _cachedSkBitmap.Width != width || _cachedSkBitmap.Height != height) {
                _cachedSurface?.Dispose();
                _cachedSkBitmap?.Dispose();
                _cachedSkBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
                _cachedSurface = SKSurface.Create(_cachedSkBitmap.Info, _cachedSkBitmap.GetPixels(), _cachedSkBitmap.RowBytes);
            }

            if (_cachedBmp == null || _cachedBmp.Width != width || _cachedBmp.Height != height) {
                _cachedBmp?.Dispose();
                _cachedBmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            }
        }

        public void Redraw() {
            if (_form.IsDisposed || !_form.IsHandleCreated || _disposed)
                return;

            //Debug.WriteLine($"[Renderer] Redraw at {DateTime.Now:HH:mm:ss.fff}");
            //Debug.WriteLine($"GDI Handles: {System.Diagnostics.Process.GetCurrentProcess().HandleCount}, MemorySize: {System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64}");

            int width = _form.Width;
            int height = _form.Height;
            EnsureBuffers(width, height);

            var canvas = _cachedSurface!.Canvas;

            DrawBackground(canvas, new Size(width, height));
            _uiElementManager.Draw(canvas);

            var bmpData = _cachedBmp!.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppPArgb);

            try {
                // Bytes direkt kopieren
                unsafe {
                    Buffer.MemoryCopy(
                        source: (void*)_cachedSkBitmap!.GetPixels(),     // Skia Speicher
                        destination: (void*)bmpData.Scan0,             // GDI Speicher
                        destinationSizeInBytes: bmpData.Stride * bmpData.Height,
                        sourceBytesToCopy: _cachedSkBitmap.ByteCount
                    );
                }
            }
            finally {
                _cachedBmp.UnlockBits(bmpData);
            }

            SetBitmap(_cachedBmp, 255);
        }

        public void SetBitmap(Bitmap bitmap, byte opacity) {
            //Debug.WriteLine($"UpdateLayeredWindow called at {DateTime.Now:HH:mm:ss.fff}");

            var screenDc = NativeMethods.GetDC(IntPtr.Zero);
            var memDc = NativeMethods.CreateCompatibleDC(screenDc);
            var hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
            var oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

            Size size = new(bitmap.Width, bitmap.Height);
            Point source = new(0, 0);
            Point topPos = new(_form.Left, _form.Top);

            var blend = new NativeMethods.BLENDFUNCTION {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = opacity,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            var success = NativeMethods.UpdateLayeredWindow(
         _form.Handle,
         screenDc,
         ref topPos,
         ref size,
         memDc,
         ref source,
         0,
         ref blend,
         NativeMethods.ULW_ALPHA);

            if (!success) {
                var err = Marshal.GetLastWin32Error();
                Debug.WriteLine($"UpdateLayeredWindow failed: {err}");
            }

            _ = NativeMethods.SelectObject(memDc, oldBitmap);
            _ = NativeMethods.DeleteObject(hBitmap);
            _ = NativeMethods.DeleteDC(memDc);
            _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }

        public void DrawBackground(SKCanvas canvas, Size size) {
            var bg = _theme.GetBackground();
            var layout = _theme.GetLayout();

            int width = size.Width;
            int height = size.Height;

            canvas.Clear(SKColors.Transparent);

            // Ecken
            canvas.DrawBitmap(bg.TopLeft, new SKPoint(0, 0));
            canvas.DrawBitmap(bg.TopRight, new SKPoint(width - bg.TopRight!.Width, 0));
            canvas.DrawBitmap(bg.BottomLeft, new SKPoint(0, height - bg.BottomLeft!.Height));
            canvas.DrawBitmap(bg.BottomRight, new SKPoint(width - bg.BottomRight!.Width, height - bg.BottomRight.Height));

            // Kanten (gestreckt)
            {
                float left   = bg.TopLeft!.Width;
                float top    = 0;
                float right  = left + (width - bg.TopLeft.Width - bg.TopRight.Width + layout.TopWidthOffset);
                float bottom = top + bg.TopCenter!.Height;
                canvas.DrawBitmap(bg.TopCenter, new SKRect(left, top, right, bottom));
            }

            {
                float left   = bg.BottomLeft.Width;
                float top    = height - bg.BottomCenter!.Height;
                float right  = left + (width - bg.BottomLeft.Width - bg.BottomRight.Width + layout.BottomWidthOffset);
                float bottom = top + bg.BottomCenter.Height;
                canvas.DrawBitmap(bg.BottomCenter, new SKRect(left, top, right, bottom));
            }

            {
                float left   = 0;
                float top    = bg.TopLeft.Height;
                float right  = left + bg.LeftCenter!.Width;
                float bottom = top + (height - bg.TopLeft.Height - bg.BottomLeft.Height + layout.LeftHeightOffset);
                canvas.DrawBitmap(bg.LeftCenter, new SKRect(left, top, right, bottom));
            }

            {
                float left   = width - bg.RightCenter!.Width;
                float top    = bg.TopRight.Height;
                float right  = left + bg.RightCenter.Width;
                float bottom = top + (height - bg.TopRight.Height - bg.BottomRight.Height + layout.RightHeightOffset);
                canvas.DrawBitmap(bg.RightCenter, new SKRect(left, top, right, bottom));
            }

            _fillPaint.Color = bg.FillColor.ToSKColor();

            {
                float left   = bg.LeftCenter.Width - layout.FillPosOffset;
                float top    = bg.TopCenter.Height - layout.FillPosOffset;
                float right  = left + (width - bg.LeftCenter.Width * 2 + layout.FillWidthOffset);
                float bottom = top + (height - bg.TopCenter.Height - bg.BottomCenter.Height + layout.FillHeightOffset);
                canvas.DrawRect(new SKRect(left, top, right, bottom), _fillPaint);
            }
        }

        // 🧹 Ressourcen-Freigabe
        public void Dispose() {
            if (_disposed)
                return;

            _renderTimer.Stop();
            _renderTimer.Dispose();

            _cachedSurface?.Dispose();
            _cachedSkBitmap?.Dispose();
            _cachedBmp?.Dispose();
            _fillPaint.Dispose();

            _cachedSurface = null;
            _cachedSkBitmap = null;
            _cachedBmp = null;
            _disposed = true;
        }
    }
}
