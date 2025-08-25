using KUpdater.Scripting;
using SkiaSharp;
using System.Drawing.Imaging;

namespace KUpdater.UI {
   public static class Renderer {
      static Renderer() { }

      public static SKColor ToSKColor(this System.Drawing.Color color) => new SKColor(color.R, color.G, color.B, color.A);
      public static SKBitmap ToSKBitmap(this Image image) {
         if (image is not Bitmap bmp) {
            // Falls es kein Bitmap ist, vorher konvertieren
            using var temp = new Bitmap(image);
            return temp.ToSKBitmap();
         }

         // Skia-Bitmap in passendem Format anlegen
         var skBmp = new SKBitmap(bmp.Width, bmp.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

         // GDI+ Bitmap sperren
         var data = bmp.LockBits(
        new Rectangle(0, 0, bmp.Width, bmp.Height),
        ImageLockMode.ReadOnly,
        PixelFormat.Format32bppPArgb // passt zu Bgra8888 + Premul
    );

         try {
            // Bytes direkt kopieren
            unsafe {
               Buffer.MemoryCopy(
                   source: (void*)data.Scan0,
                   destination: (void*)skBmp.GetPixels(),
                   destinationSizeInBytes: skBmp.ByteCount,
                   sourceBytesToCopy: bmp.Height * data.Stride
               );
            }
         }
         finally {
            bmp.UnlockBits(data);
         }

         return skBmp;
      }

      public static void DrawBackground(SKCanvas canvas, Size size) {
         var bg = LuaManager.GetBackground();
         var layout = LuaManager.GetLayout();

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
