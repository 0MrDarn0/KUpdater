using System.Runtime.InteropServices;

internal static class NativeMethods {
   public const int ULW_ALPHA = 0x00000002;
   public const byte AC_SRC_OVER = 0x00;
   public const byte AC_SRC_ALPHA = 0x01;
   public const int SW_RESTORE = 9;

   [DllImport("user32.dll", SetLastError = true)]
   public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst,
       ref Size psize, IntPtr hdcSrc, ref Point pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

   [DllImport("user32.dll")]
   public static extern IntPtr GetDC(IntPtr hWnd);

   [DllImport("user32.dll")]
   public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

   [DllImport("gdi32.dll")]
   public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

   [DllImport("gdi32.dll")]
   public static extern bool DeleteDC(IntPtr hdc);

   [DllImport("gdi32.dll")]
   public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

   [DllImport("gdi32.dll")]
   public static extern bool DeleteObject(IntPtr hObject);

   [StructLayout(LayoutKind.Sequential, Pack = 1)]
   public struct BLENDFUNCTION {
      public byte BlendOp;
      public byte BlendFlags;
      public byte SourceConstantAlpha;
      public byte AlphaFormat;
   }

   [DllImport("user32.dll")]
   public static extern bool SetForegroundWindow(IntPtr hWnd);

   [DllImport("user32.dll")]
   public static extern bool IsIconic(IntPtr hWnd);

   [DllImport("user32.dll")]
   public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


}
