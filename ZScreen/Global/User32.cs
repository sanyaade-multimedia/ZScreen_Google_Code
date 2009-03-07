﻿#region License Information (GPL v2)
/*
    ZScreen - A program that allows you to upload screenshots in one keystroke.
    Copyright (C) 2008-2009  Brandon Zimmerman

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
    
    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Windows.Forms;

namespace ZSS
{
    public static class User32
    {
        #region Variables

        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

        public const Int32 CURSOR_SHOWING = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct IconInfo
        {
            public bool fIcon;         // Specifies whether this structure defines an icon or a cursor. A value of TRUE specifies 
            public Int32 xHotspot;     // Specifies the x-coordinate of a cursor's hot spot. If this structure defines an icon, the hot 
            public Int32 yHotspot;     // Specifies the y-coordinate of the cursor's hot spot. If this structure defines an icon, the hot 
            public IntPtr hbmMask;     // (HBITMAP) Specifies the icon bitmask bitmap. If this structure defines a black and white icon, 
            public IntPtr hbmColor;    // (HBITMAP) Handle to the icon color bitmap. This member can be optional if this 
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CursorInfo
        {
            public Int32 cbSize;        // Specifies the size, in bytes, of the structure. 
            public Int32 flags;         // Specifies the cursor state. This parameter can be one of the following values:
            public IntPtr hCursor;      // Handle to the cursor. 
            public Point ptScreenPos;   // A POINT structure that receives the screen coordinates of the cursor. 
        }

        #endregion

        #region Keyboard hook

        public const int mWH_KEYBOARD_LL = 13;

        public delegate IntPtr mLowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public static mLowLevelKeyboardProc m_Proc;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            mLowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public static IntPtr setHook()
        {
            using (Process currentProc = Process.GetCurrentProcess())
            using (ProcessModule currentMod = currentProc.MainModule)
            {
                return SetWindowsHookEx(mWH_KEYBOARD_LL, m_Proc, GetModuleHandle(currentMod.ModuleName), 0);
            }
        }

        #endregion

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr ptr);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int abc);

        [DllImport("user32.dll")]
        public static extern void ReleaseDC(IntPtr dc);

        [DllImport("user32.dll")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern int GetWindowTextW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder text, int count);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CursorInfo pci);

        [DllImport("user32.dll")]
        public static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        public static extern bool GetIconInfo(IntPtr hIcon, out IconInfo piconinfo);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int EnumWindows(EnumWindowsProc ewp, int lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        public static string GetWindowLabel()
        {
            const int numOfChars = 256;
            IntPtr handle = GetForegroundWindow();
            StringBuilder sb = new StringBuilder(numOfChars);

            if (GetWindowTextW(handle, sb, numOfChars) > 0)
            {
                return sb.ToString();
            }
            else
            {
                return "";
            }
        }

        public static IntPtr GetWindowHandle()
        {
            const int numOfChars = 256;
            IntPtr handle = GetForegroundWindow();
            StringBuilder sb = new StringBuilder(numOfChars);

            if (GetWindowTextW(handle, sb, numOfChars) > 0)
            {
                return handle;
            }
            else
            {
                return IntPtr.Zero;
            }
        }

        public static MyCursor CaptureCursor()
        {
            CursorInfo ci = new CursorInfo();
            IconInfo icInfo;
            ci.cbSize = Marshal.SizeOf(ci);
            if (GetCursorInfo(out ci))
            {
                if (ci.flags == CURSOR_SHOWING)
                {
                    IntPtr hicon = CopyIcon(ci.hCursor);
                    if (GetIconInfo(hicon, out icInfo))
                    {
                        Point position = new Point(ci.ptScreenPos.X - ((int)icInfo.xHotspot),
                            ci.ptScreenPos.Y - ((int)icInfo.yHotspot));
                        Icon ic = Icon.FromHandle(hicon);
                        Bitmap bmp = ic.ToBitmap();
                        return new MyCursor(new Cursor(ci.hCursor), position, bmp);
                    }
                }
            }
            return null;
        }

        public static Image CaptureScreen(bool showCursor)
        {
            Image img = CaptureRectangle(User32.GetDesktopWindow(), MyGraphics.GetScreenBounds());
            if (showCursor)
            {
                MyCursor cursor = CaptureCursor();
                Graphics g = Graphics.FromImage(img);
                if (cursor != null)
                {
                    Rectangle rect = new Rectangle(cursor.Position, cursor.Cursor.Size);
                    cursor.Cursor.Draw(g, rect);
                }
                else
                {
                    Rectangle rect = new Rectangle(new Point(Cursor.Position.X - Cursors.Default.HotSpot.X,
                        cursor.Position.Y - Cursors.Default.HotSpot.Y), Cursors.Default.Size);
                    Cursors.Default.Draw(g, rect);
                }
            }
            return img;
        }

        public static Image CaptureRectangle(IntPtr handle, Rectangle rect)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // get the size
            int left = rect.X;
            int top = rect.Y;
            int width = rect.Width;
            int height = rect.Height;
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, left, top, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            Image img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            GDI32.DeleteObject(hBitmap);
            return img;
        }

        public static Image GrabWindow(IntPtr handle, bool showCursor)
        {
            IntPtr hdcSrc = GetWindowDC(handle);
            Rectangle windowRect = GetWindowRectangle(handle);
            Image img = new Bitmap(windowRect.Width, windowRect.Height, PixelFormat.Format32bppArgb);
            Graphics gfx = Graphics.FromImage(img);
            gfx.CopyFromScreen(windowRect.Location, new Point(0, 0), windowRect.Size, CopyPixelOperation.SourceCopy);
            if (showCursor)
            {
                MyCursor cursor = CaptureCursor();
                Graphics g = Graphics.FromImage(img);
                if (cursor != null)
                {
                    Rectangle rect = new Rectangle(new Point(cursor.Position.X - windowRect.X,
                        cursor.Position.Y - windowRect.Y), cursor.Cursor.Size);
                    cursor.Cursor.Draw(g, rect);
                }
                else
                {
                    Rectangle rect = new Rectangle(new Point(Cursor.Position.X - Cursors.Default.HotSpot.X - windowRect.X,
                        cursor.Position.Y - Cursors.Default.HotSpot.Y - windowRect.Y), Cursors.Default.Size);
                    Cursors.Default.Draw(g, rect);
                }
            }
            return img;
        }

        public class MyCursor
        {
            public Cursor Cursor;
            public Point Position;
            public Bitmap Bitmap;

            public MyCursor(Cursor cursor, Point position, Bitmap bitmap)
            {
                Cursor = cursor;
                Position = position;
                Bitmap = bitmap;
            }
        }

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_LAST
        }

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out bool pvAttribute, int cbAttribute);

        public static Rectangle DWMWA_EXTENDED_FRAME_BOUNDS(IntPtr handle)
        {
            RECT rect;
            int result = DwmGetWindowAttribute(handle, (int)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                out rect, Marshal.SizeOf(typeof(RECT)));
            if (result < 0) throw new Exception("Error: DWMWA_EXTENDED_FRAME_BOUNDS");
            return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }

        public static bool DWMWA_NCRENDERING_ENABLED(IntPtr handle)
        {
            bool enabled;
            int result = DwmGetWindowAttribute(handle, (int)DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_ENABLED,
                out enabled, sizeof(bool));
            if (result < 0) throw new Exception("Error: DWMWA_NCRENDERING_ENABLED");
            return enabled;
        }

        public static Rectangle GetWindowRect(IntPtr handle)
        {
            RECT rect;
            GetWindowRect(handle, out rect);
            return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }

        public static Rectangle GetWindowRectangle(IntPtr handle)
        {
            try
            {
                return User32.DWMWA_EXTENDED_FRAME_BOUNDS(handle);
            }
            catch
            {
                return User32.GetWindowRect(handle);
            }
        }

        public static void ActivateWindow(IntPtr handle)
        {            
            User32.SetForegroundWindow(handle);
            User32.SetActiveWindow(handle);            
        }
    }
}