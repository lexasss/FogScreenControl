using FogScreenControl.Utils;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace FogScreenControl.Services
{
    internal static class CursorManager
    {
        public static void SetInteractionCursors()
        {
            if (!_isWatchdogStarted)
            {
                StartWatchdog();
            }

            //SetCursorSize(112);

            //SetCursorFromResource($"{nameof(FogScreenControl)}.Assets.Cursors.cross.cur", WinAPI.SystemCursorIds.OCR_NORMAL);
            SetCursorFromFile(@"Assets\Cursors\crosses.cur", WinAPI.SystemCursorIds.OCR_NORMAL);
        }

        public static void RestoreDefaults()
        {
            WinAPI.SystemParametersInfo(WinAPI.SetParameterInfoActions.SETCURSORS, 0, IntPtr.Zero, 0);
            //SetCursorSize(32);
        }

        // Internal


        static bool _isWatchdogStarted = false;

        private static void StartWatchdog()
        {
            string exePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "CursorWatchdog.exe");

            if (!File.Exists(exePath))
                return;

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = Process.GetCurrentProcess().Id.ToString(),
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfo);

            _isWatchdogStarted = true;
        }

        private static void SetCursorFromFile(string path, WinAPI.SystemCursorIds cursorId)
        {
            IntPtr hCursor = WinAPI.LoadCursorFromFile(path);
            if (hCursor != IntPtr.Zero)
            {
                WinAPI.SetSystemCursor(hCursor, cursorId);
            }
            else
            {
                MessageBox.Show("Failed to load cursor.");
            }
        }

        [Obsolete("Not working: CreateIconFromResourceEx returns null")]
        private static void SetCursorFromResource(string resourceName, WinAPI.SystemCursorIds cursorId)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception("Resource not found: " + resourceName);

                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);

                IntPtr cursorIcon = WinAPI.CreateIconFromResource(bytes, (uint)bytes.Length, false, 0x00030000);

                if (cursorIcon == IntPtr.Zero)
                    throw new Exception("Failed to create cursor from resource.",
                        new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));

                IntPtr cursor = WinAPI.CopyImage(cursorIcon, WinAPI.ImageType.CURSOR, 0, 0, WinAPI.CopyImageFlags.COPYFROMRESOURCE);

                if (!WinAPI.SetSystemCursor(cursor, cursorId))
                    throw new Exception("Failed to set system cursor.",
                         new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));
            }
        }

        [Obsolete("Does not work: no reaction to changing the cursor size")]
        private static void SetCursorSize(int size)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Cursors", true))
            {
                if (key != null)
                {
                    key.SetValue("CursorBaseSize", size);

                    WinAPI.SystemParametersInfo(
                        WinAPI.SetParameterInfoActions.SETCURSORS,
                        0,
                        IntPtr.Zero,
                        WinAPI.SetParameterInfoFlags.UPDATEINIFILE | WinAPI.SetParameterInfoFlags.SENDCHANGE);
                }
            }
        }
    }
}
