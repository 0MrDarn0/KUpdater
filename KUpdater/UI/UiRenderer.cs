using KUpdater.Scripting;
using SkiaSharp;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KUpdater.UI {
   public class UIRenderer {
      private readonly Form _form;
      private readonly ITheme _theme;
      private readonly UIElementManager _uiElementManager;

      public UIRenderer(Form form, UIElementManager uiElementManager, ITheme theme) {
         _form = form;
         _uiElementManager = uiElementManager;
         _theme = theme;
      }

      public void Redraw() {
         if (_form.IsDisposed || !_form.IsHandleCreated)
            return;

         int width = _form.Width;
         int height = _form.Height;

         using var skBmp = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
         using var surface = SKSurface.Create(skBmp.Info, skBmp.GetPixels(), skBmp.RowBytes);
         var canvas = surface.Canvas;

         DrawBackground(canvas, new Size(width, height));
         _uiElementManager.Draw(canvas);

         using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
         var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
         Marshal.Copy(skBmp.Bytes, 0, bmpData.Scan0, skBmp.Bytes.Length);
         bmp.UnlockBits(bmpData);

         SetBitmap(bmp, 255);
      }

      public void SetBitmap(Bitmap bitmap, byte opacity) {
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
         canvas.DrawBitmap(bg.TopLeft.ToSKBitmap(), new SKPoint(0, 0));
         canvas.DrawBitmap(bg.TopRight.ToSKBitmap(), new SKPoint(width - bg.TopRight.Width, 0));
         canvas.DrawBitmap(bg.BottomLeft.ToSKBitmap(), new SKPoint(0, height - bg.BottomLeft.Height));
         canvas.DrawBitmap(bg.BottomRight.ToSKBitmap(), new SKPoint(width - bg.BottomRight.Width, height - bg.BottomRight.Height));

         // Kanten (gestreckt)
         {
            float left   = bg.TopLeft.Width;
            float top    = 0;
            float right  = left + (width - bg.TopLeft.Width - bg.TopRight.Width + layout.TopWidthOffset);
            float bottom = top + bg.TopCenter.Height;
            canvas.DrawBitmap(bg.TopCenter.ToSKBitmap(), new SKRect(left, top, right, bottom));
         }

         {
            float left   = bg.BottomLeft.Width;
            float top    = height - bg.BottomCenter.Height;
            float right  = left + (width - bg.BottomLeft.Width - bg.BottomRight.Width + layout.BottomWidthOffset);
            float bottom = top + bg.BottomCenter.Height;
            canvas.DrawBitmap(bg.BottomCenter.ToSKBitmap(), new SKRect(left, top, right, bottom));
         }

         {
            float left   = 0;
            float top    = bg.TopLeft.Height;
            float right  = left + bg.LeftCenter.Width;
            float bottom = top + (height - bg.TopLeft.Height - bg.BottomLeft.Height + layout.LeftHeightOffset);
            canvas.DrawBitmap(bg.LeftCenter.ToSKBitmap(), new SKRect(left, top, right, bottom));
         }

         {
            float left   = width - bg.RightCenter.Width;
            float top    = bg.TopRight.Height;
            float right  = left + bg.RightCenter.Width;
            float bottom = top + (height - bg.TopRight.Height - bg.BottomRight.Height + layout.RightHeightOffset);
            canvas.DrawBitmap(bg.RightCenter.ToSKBitmap(), new SKRect(left, top, right, bottom));
         }

         // Innenfläche
         var fillPaint = new SKPaint {
            Color = bg.FillColor.ToSKColor(),
            IsAntialias = true
         };

         {
            float left   = bg.LeftCenter.Width - layout.FillPosOffset;
            float top    = bg.TopCenter.Height - layout.FillPosOffset;
            float right  = left + (width - bg.LeftCenter.Width * 2 + layout.FillWidthOffset);
            float bottom = top + (height - bg.TopCenter.Height - bg.BottomCenter.Height + layout.FillHeightOffset);
            canvas.DrawRect(new SKRect(left, top, right, bottom), fillPaint);
         }
      }

   }
}
