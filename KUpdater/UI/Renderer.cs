// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

using System.Buffers;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using KUpdater.Scripting.Theme;
using SkiaSharp;

namespace KUpdater.UI {
    public class Renderer : IDisposable {
        private readonly Form _form;
        private readonly ITheme _theme;
        private readonly ControlManager _controlManager;
        private readonly System.Windows.Forms.Timer _renderTimer;
        private int _needsRender;
        private SKBitmap? _renderBuffer;
        private SKSurface? _renderSurface;
        private Bitmap? _backBuffer;
        private bool _disposed;
        private readonly SKPaint _fillPaint = new() { IsAntialias = true };

        public bool IsRendering { get; private set; }
        public long LastRenderDurationMs { get; private set; }
        public int LastPresentError { get; private set; }

        public Renderer(Form form, ControlManager controlManager, ITheme theme) {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _controlManager = controlManager ?? throw new ArgumentNullException(nameof(controlManager));
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
                if (_disposed || _form.IsDisposed)
                    return;
                try {
                    _form.BeginInvoke(new Action(() => {
                        if (_disposed || _form.IsDisposed)
                            return;
                        Render();
                    }));
                }
                catch (InvalidOperationException) { }
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
            _controlManager.Draw(canvas);

            var bmpData = _backBuffer!.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppPArgb);

            try {
                unsafe {
                    byte* src = (byte*)_renderBuffer!.GetPixels();
                    if (src == null)
                        return;

                    int srcRowBytes = _renderBuffer.RowBytes;
                    long srcExpectedBytes = (long)srcRowBytes * height;
                    long skByteCount = _renderBuffer.ByteCount;
                    if (skByteCount < srcExpectedBytes)
                        return;

                    byte* dst = (byte*)bmpData.Scan0;
                    if (dst == null)
                        return;

                    int dstRowBytes = bmpData.Stride;
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
                        finally { pool.Return(zeros); }
                    }

                    long dstExpectedBytes = (long)dstRowBytes * height;
                    if (srcRowBytes <= 0 || dstRowBytes <= 0 || height <= 0)
                        return;
                    if (srcExpectedBytes > Int64.MaxValue / 2 || dstExpectedBytes > Int64.MaxValue / 2)
                        return;

                    if (srcRowBytes == dstRowBytes && skByteCount >= srcExpectedBytes && dstExpectedBytes <= skByteCount) {
                        Buffer.MemoryCopy(src, dst, dstExpectedBytes, srcExpectedBytes);
                    } else {
                        int bytesPerRowToCopy = Math.Min(srcRowBytes, dstRowBytes);
                        for (int y = 0; y < height; y++) {
                            byte* sRow = src + (long)y * srcRowBytes;
                            byte* dRow = dst + (long)y * dstRowBytes;
                            long sOffset = (long)y * srcRowBytes + bytesPerRowToCopy;
                            long dOffset = (long)y * dstRowBytes + bytesPerRowToCopy;
                            if (sOffset > skByteCount || dOffset > dstExpectedBytes)
                                break;
                            Buffer.MemoryCopy(sRow, dRow, dstRowBytes, bytesPerRowToCopy);
                        }
                    }
                }
            }
            finally { _backBuffer.UnlockBits(bmpData); }

            Present(_backBuffer);
        }
        public void Present(Bitmap bitmap, byte opacity = 255) {
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

                var bmi = new NativeMethods.BITMAPINFO
                {
                    bmiHeader = new NativeMethods.BITMAPINFOHEADER
                    {
                        biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                        biWidth = width,
                        biHeight = -height,
                        biPlanes = 1,
                        biBitCount = 32,
                        biCompression = NativeMethods.BI_RGB,
                        biSizeImage = (uint)(width * height * 4)
                    },
                    bmiColors = new uint[3]
                };

                hDib = NativeMethods.CreateDIBSection(screenDc, ref bmi, NativeMethods.DIB_RGB_COLORS, out dibPixels, IntPtr.Zero, 0);
                if (hDib == IntPtr.Zero || dibPixels == IntPtr.Zero)
                    return;

                var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppPArgb);

                try {
                    unsafe {
                        byte* src = (byte*)bmpData.Scan0;
                        byte* dst = (byte*)dibPixels;
                        int srcStride = bmpData.Stride;
                        int dstStride = width * 4;

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
                finally { bitmap.UnlockBits(bmpData); }

                oldObj = NativeMethods.SelectObject(memDc, hDib);

                Size size = new(width, height);
                Point source = new(0, 0);
                Point topPos = new(_form.Left, _form.Top);

                var blend = new NativeMethods.BLENDFUNCTION
                {
                    BlendOp = NativeMethods.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = opacity,
                    AlphaFormat = NativeMethods.AC_SRC_ALPHA
                };

                bool success = NativeMethods.UpdateLayeredWindow(
                    hwnd, screenDc, ref topPos, ref size, memDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);

                if (!success) {
                    var err = Marshal.GetLastWin32Error();
                    LastPresentError = err;
                }
            }
            finally {
                if (memDc != IntPtr.Zero)
                    _ = NativeMethods.SelectObject(memDc, oldObj);
                if (hDib != IntPtr.Zero)
                    _ = NativeMethods.DeleteObject(hDib);
                if (memDc != IntPtr.Zero)
                    _ = NativeMethods.DeleteDC(memDc);
                if (screenDc != IntPtr.Zero)
                    _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        public void DrawBackground(SKCanvas canvas, Size size) {
            var bg = _theme.GetBackground();
            var layout = _theme.GetLayout();
            int width = size.Width;
            int height = size.Height;

            canvas.Clear(SKColors.Transparent);

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
                float right = left + (width - bg.TopLeft.Width - bg.TopRight.Width + layout.TopWidthOffset);
                float bottom = bg.TopCenter.Height;
                canvas.DrawBitmap(bg.TopCenter, new SKRect(left, 0, right, bottom));
            }

            if (bg.BottomCenter != null && bg.BottomLeft != null && bg.BottomRight != null) {
                float left = bg.BottomLeft.Width;
                float top = height - bg.BottomCenter.Height;
                float right = left + (width - bg.BottomLeft.Width - bg.BottomRight.Width + layout.BottomWidthOffset);
                float bottom = top + bg.BottomCenter.Height;
                canvas.DrawBitmap(bg.BottomCenter, new SKRect(left, top, right, bottom));
            }

            if (bg.LeftCenter != null && bg.TopLeft != null && bg.BottomLeft != null) {
                float top = bg.TopLeft.Height;
                float bottom = top + (height - bg.TopLeft.Height - bg.BottomLeft.Height + layout.LeftHeightOffset);
                canvas.DrawBitmap(bg.LeftCenter, new SKRect(0, top, bg.LeftCenter.Width, bottom));
            }

            if (bg.RightCenter != null && bg.TopRight != null && bg.BottomRight != null) {
                float left = width - bg.RightCenter.Width;
                float top = bg.TopRight.Height;
                float bottom = top + (height - bg.TopRight.Height - bg.BottomRight.Height + layout.RightHeightOffset);
                canvas.DrawBitmap(bg.RightCenter, new SKRect(left, top, left + bg.RightCenter.Width, bottom));
            }

            _fillPaint.Color = bg.FillColor.ToSKColor();
            float fillLeft = bg.LeftCenter!.Width - layout.FillPosOffset;
            float fillTop = bg.TopCenter!.Height - layout.FillPosOffset;
            float fillRight = fillLeft + (width - bg.LeftCenter.Width * 2 + layout.FillWidthOffset);
            float fillBottom = fillTop + (height - bg.TopCenter.Height - bg.BottomCenter!.Height + layout.FillHeightOffset);
            canvas.DrawRect(new SKRect(fillLeft, fillTop, fillRight, fillBottom), _fillPaint);
        }

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
            _disposed = true;
        }
    }
}
