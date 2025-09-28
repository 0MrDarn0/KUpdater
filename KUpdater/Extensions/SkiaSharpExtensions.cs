using System.Drawing.Imaging;
using SkiaSharp;

public static class SkiaSharpExtensions {

   public static SKColor ToSKColor(this System.Drawing.Color color)
      => new(color.R, color.G, color.B, color.A);


   public static SKBitmap ToSKBitmap(this Image image) {
      if (image is not Bitmap bmp) {
         // Falls es kein Bitmap ist, vorher konvertieren
         using var temp = new Bitmap(image);
         return temp.ToSKBitmap();
      }

      // Skia-Bitmap in passendem Format anlegen
      SKBitmap skBmp = new(bmp.Width, bmp.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

      // GDI+ Bitmap sperren
      var data = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppPArgb);

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
}