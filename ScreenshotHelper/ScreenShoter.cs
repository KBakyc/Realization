using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace ScreenshotHelper
{
    public static class ScreenShoter
    {
        public static bool CaptureToFile(string _path)
        {
            if (String.IsNullOrEmpty(_path)) return false;

            bool res = false;

            var bmp = GetScreenBitmap();
            if (bmp != null)
            {
                try
                {
                    bmp.Save(_path, System.Drawing.Imaging.ImageFormat.Png);
                    res = true;
                }
                catch
                {
                    res = false;
                }
                finally
                {
                    bmp.Dispose();
                }
            }            
            return res;
        }

        public static Stream CaptureToStream()
        {
            Stream res = null;
            
            var bmp = GetScreenBitmap();
            if (bmp != null)
            {
                try
                {
                    res = new MemoryStream();
                    bmp.Save(res, System.Drawing.Imaging.ImageFormat.Png);
                    res.Position = 0L;
                }
                catch
                {
                    if (res != null)
                    {
                        res.Dispose();
                        res = null;
                    }
                }
                finally
                {
                    bmp.Dispose();
                }
            }         

            return res;
        }

        private static Bitmap GetScreenBitmap()
        {
            Bitmap bmp = null;
            
            try
            {
                Size sz = Screen.PrimaryScreen.Bounds.Size;
                IntPtr hDesk = GetDesktopWindow();
                IntPtr hSrce = GetWindowDC(hDesk);
                IntPtr hDest = CreateCompatibleDC(hSrce);
                IntPtr hBmp = CreateCompatibleBitmap(hSrce, sz.Width, sz.Height);
                IntPtr hOldBmp = SelectObject(hDest, hBmp);
                bool b = BitBlt(hDest, 0, 0, sz.Width, sz.Height, hSrce, 0, 0, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);
                bmp = Bitmap.FromHbitmap(hBmp);
                SelectObject(hDest, hOldBmp);
                DeleteObject(hBmp);
                DeleteDC(hDest);
                ReleaseDC(hDesk, hSrce);
            }
            catch
            { }

            return bmp;
        }

        // P/Invoke declarations
        [DllImport("gdi32.dll")]
        static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, System.Drawing.CopyPixelOperation rop);
        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);
        [DllImport("gdi32.dll")]
        static extern IntPtr DeleteDC(IntPtr hDc);
        [DllImport("gdi32.dll")]
        static extern IntPtr DeleteObject(IntPtr hDc);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);
        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr ptr);
    }
}
