// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using KUpdater.Scripting.Theme;
using SkiaSharp;

namespace KUpdater.UI {
    // Renderer: responsible for composing with Skia and presenting via Win32 layered window APIs.
    // Key responsibilities: manage buffers, perform Skia drawing, copy pixels into a GDI bitmap,
    // and call UpdateLayeredWindow to present the result.
    public class Renderer : IDisposable {
        private readonly Form _form;                      // host form (used for HWND, size, position)
        private readonly ITheme _theme;                   // theming source used for background and layout
        private readonly ControlManager _uiElementManager;// draws UI elements into the Skia canvas
        private readonly System.Windows.Forms.Timer _renderTimer; // main render tick (rate-limited)
        private int _needsRender;                         // atomic flag (0/1) to request a render
        private SKBitmap? _renderBuffer;                  // Skia CPU bitmap that we draw onto
        private SKSurface? _renderSurface;                // Skia surface referencing _renderBuffer
        private Bitmap? _backBuffer;                      // GDI backbuffer (Format32bppPArgb) used for HBITMAP
        private bool _disposed;                           // disposal guard
        private readonly SKPaint _fillPaint = new() { IsAntialias = true }; // reused paint for fills

        // Optional: HBITMAP cache to reduce GetHbitmap/DeleteObject churn (map key = width<<32 | height)
        private readonly ConcurrentDictionary<long, IntPtr> _hBitmapCache = new();

        // Diagnostics exposed for debugging/profiling
        public bool IsRendering { get; private set; }
        public long LastRenderDurationMs { get; private set; }
        public int LastPresentError { get; private set; }

        // ctor: validate args and start render timer
        public Renderer(Form form, ControlManager uiElementManager, ITheme theme) {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _uiElementManager = uiElementManager ?? throw new ArgumentNullException(nameof(uiElementManager));
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));

            // Timer controls render cadence; keep it simple (33ms ≈ 30Hz)
            _renderTimer = new System.Windows.Forms.Timer { Interval = 33 };
            _renderTimer.Tick += RenderTimer_Tick;
            _renderTimer.Start();
        }

        // Thread-safe render request: atomic set so multiple callers don't cause races
        public void RequestRender() => Interlocked.Exchange(ref _needsRender, 1);
        private void RenderTimer_Tick(object? sender, EventArgs e) => RenderTick();

        // RenderTick: drains the atomic flag, marshals to UI thread when required,
        // applies any theme state, then calls Render() which performs the heavy work.
        private void RenderTick() {
            // If flag was not set, nothing to do
            if (Interlocked.Exchange(ref _needsRender, 0) == 0)
                return;

            if (_disposed || _form.IsDisposed)
                return;

            // Drawing/presentation touches WinForms/Win32 handles; ensure UI thread
            if (_form.InvokeRequired) {
                if (_disposed || _form.IsDisposed)
                    return;
                try {
                    // Use BeginInvoke to avoid blocking caller; Render will check handle state again
                    _form.BeginInvoke(new Action(() => {
                        if (_disposed || _form.IsDisposed)
                            return;
                        Render();
                    }));
                }
                catch (InvalidOperationException) {
                    // Form is closing/disposed; swallow safely
                }
                return;
            }

            IsRendering = true;
            var sw = Stopwatch.StartNew();
            try {
                // Allow theme implementations to finalize transient state before drawing
                (_theme as ThemeBase)?.ApplyLastState();
                Render();
            }
            finally {
                sw.Stop();
                LastRenderDurationMs = sw.ElapsedMilliseconds;
                IsRendering = false;
            }
        }

        // Compute device pixel size. This is a basic approach that multiplies logical size
        // by Control.DeviceDpi / 96. For per-monitor DPI handling you may need Win32 calls.
        private void GetDeviceSize(out int deviceWidth, out int deviceHeight) {
            float scale = Math.Max(1f, _form.DeviceDpi / 96f);
            deviceWidth = (int)Math.Ceiling(_form.Width * scale);
            deviceHeight = (int)Math.Ceiling(_form.Height * scale);
            if (deviceWidth <= 0)
                deviceWidth = 1;
            if (deviceHeight <= 0)
                deviceHeight = 1;
        }

        // EnsureBuffers: create or resize Skia and GDI buffers when size changes.
        // Important details:
        // - SKBitmap uses BGRA premultiplied which aligns with Skia expectations.
        // - Backbuffer uses Format32bppPArgb (premultiplied alpha) compatible with UpdateLayeredWindow.
        public void EnsureBuffers(int width, int height) {
            if (width <= 0 || height <= 0)
                return;

            if (_renderBuffer == null || _renderBuffer.Width != width || _renderBuffer.Height != height) {
                _renderSurface?.Dispose();
                _renderBuffer?.Dispose();
                _renderBuffer = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
                // Create a surface that directly uses SKBitmap pixel memory to avoid extra copies
                _renderSurface = SKSurface.Create(_renderBuffer.Info, _renderBuffer.GetPixels(), _renderBuffer.RowBytes);
            }

            if (_backBuffer == null || _backBuffer.Width != width || _backBuffer.Height != height) {
                _backBuffer?.Dispose();
                _backBuffer = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            }
        }

        // Render: perform Skia drawing, copy into GDI backbuffer safely (stride-aware),
        // then call Present() to perform Win32 layered window update.
        public void Render() {
            if (_form.IsDisposed || !_form.IsHandleCreated || _disposed)
                return;

            // Use device pixels to match what UpdateLayeredWindow expects on high-DPI setups
            GetDeviceSize(out int width, out int height);
            EnsureBuffers(width, height);

            var canvas = _renderSurface!.Canvas;

            // Draw background and UI
            DrawBackground(canvas, new Size(width, height));
            _uiElementManager.Draw(canvas);

            // Lock bits of GDI backbuffer for direct memory copy
            var bmpData = _backBuffer!.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppPArgb);

            try {
                // Stride-aware safe copy from Skia memory to GDI memory
                unsafe {
                    byte* src = (byte*)_renderBuffer!.GetPixels();
                    int srcRowBytes = _renderBuffer.RowBytes;
                    byte* dst = (byte*)bmpData.Scan0;
                    int dstRowBytes = bmpData.Stride;

                    // If destination stride is larger than source, zero the extra bytes to avoid garbage pixels.
                    // This prevents visual artifacts when Format/stride differ.
                    if (dstRowBytes > srcRowBytes) {
                        // clears dst rows before partial copies to avoid residual data.
                        // Uses ArrayPool to avoid allocating frequently.
                        var pool = ArrayPool<byte>.Shared;
                        byte[] zeros = pool.Rent(dstRowBytes);
                        try {
                            Array.Clear(zeros, 0, dstRowBytes);
                            for (int y = 0; y < height; y++) {
                                IntPtr rowPtr = new IntPtr((byte*)dst + (long)y * dstRowBytes);
                                Marshal.Copy(zeros, 0, rowPtr, dstRowBytes);
                            }
                        }
                        finally {
                            pool.Return(zeros);
                        }
                    }

                    if (srcRowBytes == dstRowBytes) {
                        // Fast path: bulk copy if strides match
                        Buffer.MemoryCopy(src, dst, (long)dstRowBytes * height, (long)srcRowBytes * height);
                    } else {
                        // Fallback: copy per row, copying the minimum number of bytes
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

            // Present the resulting backbuffer to the layered window
            PresentWithDib(_backBuffer);
        }

        // Present: wraps GDI calls to create HBITMAP, select into DC, call UpdateLayeredWindow,
        // and clean up native resources. Implementation is defensive and logs Win32 errors.
        public void Present(Bitmap bitmap, byte opacity = 255) {
            if (_disposed || bitmap == null)
                return;

            if (_form.IsDisposed || !_form.IsHandleCreated)
                return;

            // re-check HWND just before presenting to handle races where form moved/disposed
            IntPtr hwnd = _form.Handle;
            if (hwnd == IntPtr.Zero)
                return;

            IntPtr screenDc = IntPtr.Zero;
            IntPtr memDc = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            LastPresentError = 0;

            // Cache key for HBITMAP reuse: combine width and height to form 64-bit key
            long key = ((long)bitmap.Width << 32) | (uint)bitmap.Height;

            try {
                screenDc = NativeMethods.GetDC(IntPtr.Zero);
                if (screenDc == IntPtr.Zero)
                    return;

                memDc = NativeMethods.CreateCompatibleDC(screenDc);
                if (memDc == IntPtr.Zero)
                    return;

                // We try to reuse an HBITMAP to reduce GDI pressure; if not present create a new one.
                if (!_hBitmapCache.TryGetValue(key, out hBitmap) || hBitmap == IntPtr.Zero) {
                    hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                    if (hBitmap != IntPtr.Zero) {
                        _hBitmapCache[key] = hBitmap;
                    }
                } else {
                    // If a cached HBITMAP exists, get a fresh HBITMAP and replace cache to keep pixels in sync.
                    var newH = bitmap.GetHbitmap(Color.FromArgb(0));
                    if (newH != IntPtr.Zero) {
                        _hBitmapCache[key] = newH;
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

                // Use current form location for top-left of the layered window
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
                // Always restore the original bitmap into the DC
                if (memDc != IntPtr.Zero) {
                    _ = NativeMethods.SelectObject(memDc, oldBitmap);
                }

                // If we did not cache the created HBITMAP, delete it immediately; otherwise keep for reuse.
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

        public void PresentWithDib(Bitmap bitmap, byte opacity = 255) {
            if (_disposed || bitmap == null)
                return;
            if (_form.IsDisposed || !_form.IsHandleCreated)
                return;

            IntPtr hwnd = _form.Handle;
            if (hwnd == IntPtr.Zero)
                return;

            IntPtr screenDc = IntPtr.Zero;
            IntPtr memDc = IntPtr.Zero;
            IntPtr hDib = IntPtr.Zero;
            IntPtr oldObj = IntPtr.Zero;
            IntPtr dibPixels = IntPtr.Zero;

            try {
                screenDc = NativeMethods.GetDC(IntPtr.Zero);
                if (screenDc == IntPtr.Zero)
                    return;

                memDc = NativeMethods.CreateCompatibleDC(screenDc);
                if (memDc == IntPtr.Zero)
                    return;

                int width = bitmap.Width;
                int height = bitmap.Height;

                // Build BITMAPINFO for 32bpp BGRA (no palette)
                var bmi = new NativeMethods.BITMAPINFO {
                    bmiHeader = new NativeMethods.BITMAPINFOHEADER {
                        biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                        biWidth = width,
                        biHeight = -height, // negative to create a top-down DIB (scanlines top->bottom)
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = NativeMethods.BI_RGB,
                        biSizeImage = (uint)(width * height * 4),
                        biXPelsPerMeter = 0,
                        biYPelsPerMeter = 0,
                        biClrUsed = 0,
                        biClrImportant = 0
                    },
                    bmiColors = new uint[3]
                };

                // Create DIB section; returns pointer to pixel memory
                hDib = NativeMethods.CreateDIBSection(screenDc, ref bmi, NativeMethods.DIB_RGB_COLORS, out dibPixels, IntPtr.Zero, 0);
                if (hDib == IntPtr.Zero || dibPixels == IntPtr.Zero)
                    return;

                // Copy pixels from Skia/backbuffer into dibPixels
                // We expect Skia buffer in BGRA premultiplied format and bitmap PixelFormat.Format32bppPArgb target.
                var bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                try {
                    unsafe {
                        byte* src = (byte*)bmpData.Scan0;
                        byte* dst = (byte*)dibPixels;
                        int srcStride = bmpData.Stride;
                        int dstStride = width * 4; // since we set 32bpp and top-down, it's width*4
                        if (srcStride == dstStride) {
                            Buffer.MemoryCopy(src, dst, (long)dstStride * height, (long)srcStride * height);
                        } else {
                            for (int y = 0; y < height; y++) {
                                byte* sRow = src + (long)y * srcStride;
                                byte* dRow = dst + (long)y * dstStride;
                                int bytesToCopy = Math.Min(srcStride, dstStride);
                                Buffer.MemoryCopy(sRow, dRow, dstStride, bytesToCopy);
                            }
                        }
                    }
                }
                finally {
                    bitmap.UnlockBits(bmpData);
                }

                // Select DIB into DC
                oldObj = NativeMethods.SelectObject(memDc, hDib);

                Size size = new(width, height);
                Point source = new(0, 0);
                Point topPos = new(_form.Left, _form.Top);
                var blend = new NativeMethods.BLENDFUNCTION {
                    BlendOp = NativeMethods.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = opacity,
                    AlphaFormat = NativeMethods.AC_SRC_ALPHA
                };

                bool success = NativeMethods.UpdateLayeredWindow(hwnd, screenDc, ref topPos, ref size, memDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);
                if (!success) {
                    var err = Marshal.GetLastWin32Error();
                    Debug.WriteLine($"UpdateLayeredWindow failed: {err}");
                    LastPresentError = err;
                }
            }
            finally {
                if (memDc != IntPtr.Zero) {
                    _ = NativeMethods.SelectObject(memDc, oldObj);
                }
                if (hDib != IntPtr.Zero) {
                    _ = NativeMethods.DeleteObject(hDib);
                }
                if (memDc != IntPtr.Zero) {
                    _ = NativeMethods.DeleteDC(memDc);
                }
                if (screenDc != IntPtr.Zero) {
                    _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
                }
            }
        }


        // DrawBackground: composes theme images into the given canvas.
        // Defensive checks are present to avoid NREs with faulty themes.
        public void DrawBackground(SKCanvas canvas, Size size) {
            var bg = _theme.GetBackground();
            var layout = _theme.GetLayout();

            int width = size.Width;
            int height = size.Height;

            canvas.Clear(SKColors.Transparent);

            // Corners – check for null to be defensive
            if (bg.TopLeft != null)
                canvas.DrawBitmap(bg.TopLeft, new SKPoint(0, 0));
            if (bg.TopRight != null)
                canvas.DrawBitmap(bg.TopRight, new SKPoint(width - bg.TopRight.Width, 0));
            if (bg.BottomLeft != null)
                canvas.DrawBitmap(bg.BottomLeft, new SKPoint(0, height - bg.BottomLeft.Height));
            if (bg.BottomRight != null)
                canvas.DrawBitmap(bg.BottomRight, new SKPoint(width - bg.BottomRight.Width, height - bg.BottomRight.Height));

            // Edges – each block verifies presence of required parts to avoid exceptions
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
                // Fill rect uses offsets from layout to adapt to various border sizes
                float left = bg.LeftCenter!.Width - layout.FillPosOffset;
                float top = bg.TopCenter!.Height - layout.FillPosOffset;
                float right = left + (width - bg.LeftCenter.Width * 2 + layout.FillWidthOffset);
                float bottom = top + (height - bg.TopCenter.Height - bg.BottomCenter!.Height + layout.FillHeightOffset);
                canvas.DrawRect(new SKRect(left, top, right, bottom), _fillPaint);
            }
        }

        // Dispose: detach timer, stop it, dispose Skia/GDI resources and free any cached HBITMAPs.
        public void Dispose() {
            if (_disposed)
                return;

            // Prevent further render requests
            _needsRender = 0;

            _renderTimer.Tick -= RenderTimer_Tick;
            try { _renderTimer.Stop(); }
            catch { /* ignore */ }
            _renderTimer.Dispose();

            _renderSurface?.Dispose();
            _renderBuffer?.Dispose();
            _backBuffer?.Dispose();
            _fillPaint.Dispose();

            _renderSurface = null;
            _renderBuffer = null;
            _backBuffer = null;

            // Free cached HBITMAPs to avoid GDI leaks when renderer is disposed
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
