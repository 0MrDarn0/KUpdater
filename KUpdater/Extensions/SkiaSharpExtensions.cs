// Copyright (c) 2025 Christian Schnuck - Licensed under the GPL-3.0 (see LICENSE.txt)

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

        if (bmp.PixelFormat != PixelFormat.Format32bppPArgb)
            bmp = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), PixelFormat.Format32bppPArgb);


        SKBitmap skBmp = new(bmp.Width, bmp.Height, SKColorType.Bgra8888, SKAlphaType.Premul);


        var data = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppPArgb);

        try {
            unsafe {
                int bytesToCopy = Math.Min(bmp.Height * data.Stride, skBmp.ByteCount);
                Buffer.MemoryCopy(
                    source: (void*)data.Scan0,
                    destination: (void*)skBmp.GetPixels(),
                    destinationSizeInBytes: skBmp.ByteCount,
                    sourceBytesToCopy: bytesToCopy);
            }
        }
        finally {
            bmp.UnlockBits(data);
        }
        return skBmp;
    }
}
