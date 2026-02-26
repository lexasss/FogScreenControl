using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CursorWatchdog
{
    internal class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        const uint SPI_SETCURSORS = 0x0057;
        const uint SPIF_UPDATEINIFILE = 0x01;
        const uint SPIF_SENDCHANGE = 0x02;

        private static void Main(string[] args)
        {
            if (args.Length != 1 || !int.TryParse(args[0], out int parentPid))
                return;

            try
            {
                Process parent = Process.GetProcessById(parentPid);

                // Wait until the main app exits
                parent.WaitForExit();

                /* Altrenatively:
                while (!parent.HasExited)
                {
                    Thread.Sleep(1000);
                }
                */
            }
            catch
            {
                // If we can't access the process, assume it's already gone
            }

            // Small delay to ensure process fully terminated
            Thread.Sleep(500);

            /* Cursor scaling does not work in the main app
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Cursors", true))
            {
                if (key != null)
                {
                    key.SetValue("CursorBaseSize", 32);
                }
            }*/

            SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
    }
}
