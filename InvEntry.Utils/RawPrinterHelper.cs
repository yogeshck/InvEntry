using System;
using System.Runtime.InteropServices;

namespace InvEntry.Utils
{
    public class RawPrinterHelper
    {
        
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool OpenPrinter(string pPrinterName, out IntPtr hPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOC_INFO_1 di);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential)]
        public struct DOC_INFO_1
        {
            public string pDocName;
            public string pOutputFile;
            public string pDataType;
        }

        public static bool SendZPLToPrinter(string printerName, string zplData, out string err)
        {
            err = null;

            IntPtr hPrinter;
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                err = "Failed to open printer.";
                return false;
            }

            DOC_INFO_1 di = new DOC_INFO_1
            {
                pDocName = "ZPL Print Job",
                pDataType = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, ref di))
            {
                ClosePrinter(hPrinter);
                err = "Cannot start printer";
                return false;
            }

            if (!StartPagePrinter(hPrinter))
            {
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);
                err = "Cannot start Page printer";
                return false;
            }

            IntPtr pBytes = Marshal.StringToCoTaskMemAnsi(zplData);
            int dwWritten;
            bool success = WritePrinter(hPrinter, pBytes, zplData.Length, out dwWritten);
            Marshal.FreeCoTaskMem(pBytes);

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
            ClosePrinter(hPrinter);

            return success;
        }
    }
}
