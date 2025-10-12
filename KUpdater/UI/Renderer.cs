// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Buffers;
using System.Collections.Concurrent;
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
        private int _needsRender;// 0 = false, 1 = true (atomic)
        private SKBitmap? _renderBuffer;
        private SKSurface? _renderSurface;
        private Bitmap? _backBuffer;
        private bool _disposed;
        private readonly SKPaint _fillPaint = new() { IsAntialias = true };

        // HBitmap cache to reduce GDI pressure; key = width<<32 | height
        private readonly ConcurrentDictionary<long, IntPtr> _hBitmapCache = new();

        public bool IsRendering { get; private set; }
        public long LastRenderDurationMs { get; private set; }
        public int LastPresentError { get; private set; }


        public Renderer(Form form, ControlManager uiElementManager, ITheme theme) {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _uiElementManager = uiElementManager ?? throw new ArgumentNullException(nameof(uiElementManager));
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));

            _renderTimer = new System.Windows.Forms.Timer { Interval = 33 };
            _renderTimer.Tick += RenderTimer_Tick;
            _renderTimer.Start();
        }

        public void RequestRender() => Interlocked.Exchange(ref _needsRender, 1);
        private void RenderTimer_Tick(object? sender, EventArgs e) => RenderTick();

        private void RenderTick() {
            if (Interlocked.Exchange(ref _needsRender, 0) == 0)
                return;

            if (_disposed || _form.IsDisposed)
                return;


            if (_form.InvokeRequired) {
                try {
                    _form.BeginInvoke(new Action(Render));
                }
                catch (InvalidOperationException) {
                    // form closing/disposed; ignore
                }
                return;
            }

            IsRendering = true;
            var sw = Stopwatch.StartNew();
            try {
                (_theme as ThemeBase)?.ApplyLastState();
                Render();
            }
            finally {
                sw.Stop();
                LastRenderDurationMs = sw.ElapsedMilliseconds;
                IsRendering = false;
            }
        }

        // Calculate device pixel size (basic: uses Control.DeviceDpi)
        private void GetDeviceSize(out int deviceWidth, out int deviceHeight) {
            float scale = Math.Max(1f, _form.DeviceDpi / 96f);
            deviceWidth = (int)Math.Ceiling(_form.Width * scale);
            deviceHeight = (int)Math.Ceiling(_form.Height * scale);
            if (deviceWidth <= 0)
                deviceWidth = 1;
            if (deviceHeight <= 0)
                deviceHeight = 1;
        }

        public void EnsureBuffers(int width, int height) {
            if (width <= 0 || height <= 0)
                return;

            if (_renderBuffer == null || _renderBuffer.Width != width || _renderBuffer.Height != height) {
                _renderSurface?.Dispose();
                _renderBuffer?.Dispose();
                _renderBuffer = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
                _renderSurface = SKSurface.Create(_renderBuffer.Info, _renderBuffer.GetPixels(), _renderBuffer.RowBytes);
            }

            if (_backBuffer == null || _backBuffer.Width != width || _backBuffer.Height != height) {
                _backBuffer?.Dispose();
                _backBuffer = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            }
        }

        public void Render() {
            if (_form.IsDisposed || !_form.IsHandleCreated || _disposed)
                return;

            GetDeviceSize(out int width, out int height);
            EnsureBuffers(width, height);

            var canvas = _renderSurface!.Canvas;

            DrawBackground(canvas, new Size(width, height));
            _uiElementManager.Draw(canvas);

            var bmpData = _backBuffer!.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppPArgb);

            try {
                // Sicherer, stride-bewusster Kopiervorgang
                unsafe {
                    byte* src = (byte*)_renderBuffer!.GetPixels();
                    int srcRowBytes = _renderBuffer.RowBytes;
                    byte* dst = (byte*)bmpData.Scan0;
                    int dstRowBytes = bmpData.Stride;

                    // If target has larger stride, zero the extra bytes to avoid garbage
                    if (dstRowBytes > srcRowBytes) {
                        var pool = ArrayPool<byte>.Shared;
                        byte[] zeros = pool.Rent(dstRowBytes);
                        try {
                            Array.Clear(zeros, 0, dstRowBytes);
                            for (int y = 0; y < height; y++) {
                                IntPtr rowPtr = new((byte*)dst + (long)y * dstRowBytes);
                                Marshal.Copy(zeros, 0, rowPtr, dstRowBytes);
                            }
                        }
                        finally {
                            pool.Return(zeros);
                        }
                    }

                    if (srcRowBytes == dstRowBytes) {
                        Buffer.MemoryCopy(src, dst, (long)dstRowBytes * height, (long)srcRowBytes * height);
                    } else {
                        for (int y = 0; y < height; y++) {
                            byte* sRow = src + (long)y * srcRowBytes;
                            byte* dRow = dst + (long)y * dstRowBytes;
                            int bytesToCopy = Math.Min(srcRowBytes, dstRowBytes);
                            Buffer.MemoryCopy(sRow, dRow, dstRowBytes, bytesToCopy);
                        }
                    }
                }
            }
            finally {
                _backBuffer.UnlockBits(bmpData);
            }

            Present(_backBuffer);
        }

        public void Present(Bitmap bitmap, byte opacity = 255) {
            if (_disposed || bitmap == null)
                return;

            if (_form.IsDisposed || !_form.IsHandleCreated)
                return;

            // re-check handle just before presenting
            IntPtr hwnd = _form.Handle;
            if (hwnd == IntPtr.Zero)
                return;

            IntPtr screenDc = IntPtr.Zero;
            IntPtr memDc = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            LastPresentError = 0;

            long key = ((long)bitmap.Width << 32) | (uint)bitmap.Height;

            try {
                screenDc = NativeMethods.GetDC(IntPtr.Zero);
                if (screenDc == IntPtr.Zero)
                    return;

                memDc = NativeMethods.CreateCompatibleDC(screenDc);
                if (memDc == IntPtr.Zero)
                    return;

                // Create HBITMAP with background 0 = transparent
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                if (hBitmap == IntPtr.Zero)
                    return;

                // Try reuse cached HBITMAP for identical size
                if (!_hBitmapCache.TryGetValue(key, out hBitmap) || hBitmap == IntPtr.Zero) {
                    hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                    if (hBitmap != IntPtr.Zero) {
                        // store cache only if successful; if collisions occur we'll overwrite
                        _hBitmapCache[key] = hBitmap;
                    }
                } else {
                    // If we reuse a cached HBITMAP, we need to update its pixels.
                    // Easiest and safest approach is to create a new HBITMAP from bitmap and replace cache.
                    // This avoids complex GDI bitmap pixel update code.
                    var newH = bitmap.GetHbitmap(Color.FromArgb(0));
                    if (newH != IntPtr.Zero) {
                        // replace cached handle atomically
                        _hBitmapCache[key] = newH;
                        // keep hBitmap to delete old after select
                        var toDelete = hBitmap;
                        hBitmap = newH;
                        if (toDelete != IntPtr.Zero) {
                            _ = NativeMethods.DeleteObject(toDelete);
                        }
                    }
                }

                if (hBitmap == IntPtr.Zero)
                    return;

                oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

                Size size = new(bitmap.Width, bitmap.Height);
                Point source = new(0, 0);

                // Use form position in screen coordinates; re-evaluate to handle move during render
                Point topPos = new(_form.Left, _form.Top);

                var blend = new NativeMethods.BLENDFUNCTION {
                    BlendOp = NativeMethods.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = opacity,
                    AlphaFormat = NativeMethods.AC_SRC_ALPHA
                };

                bool success = NativeMethods.UpdateLayeredWindow(
                    hwnd,
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
                    LastPresentError = err;
                    Debug.WriteLine($"UpdateLayeredWindow failed: {err}");
                }
            }
            finally {
                // Restore old bitmap into DC before deleting ours
                if (memDc != IntPtr.Zero) {
                    _ = NativeMethods.SelectObject(memDc, oldBitmap);
                }

                // Do not delete the cached hBitmap here; cached handles are kept for reuse.
                // However if the cache does not contain this key (we created a temporary) delete it.
                if (_hBitmapCache.TryGetValue(((long)bitmap.Width << 32) | (uint)bitmap.Height, out var cached) == false) {
                    if (hBitmap != IntPtr.Zero) {
                        _ = NativeMethods.DeleteObject(hBitmap);
                    }
                }

                if (memDc != IntPtr.Zero) {
                    _ = NativeMethods.DeleteDC(memDc);
                }
                if (screenDc != IntPtr.Zero) {
                    _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
                }
            }
        }

        public void DrawBackground(SKCanvas canvas, Size size) {
            var bg = _theme.GetBackground();
            var layout = _theme.GetLayout();

            int width = size.Width;
            int height = size.Height;

            canvas.Clear(SKColors.Transparent);

            // Ecken
            // defensive null-checks to avoid runtime exceptions in faulty themes
            if (bg.TopLeft != null)
                canvas.DrawBitmap(bg.TopLeft, new SKPoint(0, 0));
            if (bg.TopRight != null)
                canvas.DrawBitmap(bg.TopRight, new SKPoint(width - bg.TopRight.Width, 0));
            if (bg.BottomLeft != null)
                canvas.DrawBitmap(bg.BottomLeft, new SKPoint(0, height - bg.BottomLeft.Height));
            if (bg.BottomRight != null)
                canvas.DrawBitmap(bg.BottomRight, new SKPoint(width - bg.BottomRight.Width, height - bg.BottomRight.Height));

            if (bg.TopCenter != null && bg.TopLeft != null && bg.TopRight != null) {
                float left = bg.TopLeft.Width;
                float top = 0;
                float right = left + (width - bg.TopLeft.Width - bg.TopRight.Width + layout.TopWidthOffset);
                float bottom = top + bg.TopCenter.Height;
                canvas.DrawBitmap(bg.TopCenter, new SKRect(left, top, right, bottom));
            }

            if (bg.BottomCenter != null && bg.BottomLeft != null && bg.BottomRight != null) {
                float left = bg.BottomLeft.Width;
                float top = height - bg.BottomCenter.Height;
                float right = left + (width - bg.BottomLeft.Width - bg.BottomRight.Width + layout.BottomWidthOffset);
                float bottom = top + bg.BottomCenter.Height;
                canvas.DrawBitmap(bg.BottomCenter, new SKRect(left, top, right, bottom));
            }

            if (bg.LeftCenter != null && bg.TopLeft != null && bg.BottomLeft != null) {
                float left = 0;
                float top = bg.TopLeft.Height;
                float right = left + bg.LeftCenter.Width;
                float bottom = top + (height - bg.TopLeft.Height - bg.BottomLeft.Height + layout.LeftHeightOffset);
                canvas.DrawBitmap(bg.LeftCenter, new SKRect(left, top, right, bottom));
            }

            if (bg.RightCenter != null && bg.TopRight != null && bg.BottomRight != null) {
                float left = width - bg.RightCenter.Width;
                float top = bg.TopRight.Height;
                float right = left + bg.RightCenter.Width;
                float bottom = top + (height - bg.TopRight.Height - bg.BottomRight.Height + layout.RightHeightOffset);
                canvas.DrawBitmap(bg.RightCenter, new SKRect(left, top, right, bottom));
            }

            _fillPaint.Color = bg.FillColor.ToSKColor();

            {
                float left = bg.LeftCenter!.Width - layout.FillPosOffset;
                float top = bg.TopCenter!.Height - layout.FillPosOffset;
                float right = left + (width - bg.LeftCenter.Width * 2 + layout.FillWidthOffset);
                float bottom = top + (height - bg.TopCenter.Height - bg.BottomCenter!.Height + layout.FillHeightOffset);
                canvas.DrawRect(new SKRect(left, top, right, bottom), _fillPaint);
            }
        }

        // 🧹 Ressourcen-Freigabe
        public void Dispose() {
            if (_disposed)
                return;

            _needsRender = 0;

            _renderTimer.Tick -= RenderTimer_Tick;
            try { _renderTimer.Stop(); }
            catch { }
            _renderTimer.Dispose();

            _renderSurface?.Dispose();
            _renderBuffer?.Dispose();
            _backBuffer?.Dispose();
            _fillPaint.Dispose();

            _renderSurface = null;
            _renderBuffer = null;
            _backBuffer = null;

            // free cached HBITMAPs
            foreach (var kv in _hBitmapCache) {
                if (kv.Value != IntPtr.Zero) {
                    _ = NativeMethods.DeleteObject(kv.Value);
                }
            }
            _hBitmapCache.Clear();

            _disposed = true;
        }
    }
}
