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

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetPrinter(IntPtr hPrinter, int level, IntPtr pPrinter, int cbBuf, out int pcbNeeded);

        [StructLayout(LayoutKind.Sequential)]
        public struct DOC_INFO_1
        {
            public string pDocName;
            public string pOutputFile;
            public string pDataType;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PRINTER_INFO_2
        {
            public string pServerName;
            public string pPrinterName;
            public string pShareName;
            public string pPortName;
            public string pDriverName;
            public string pComment;
            public string pLocation;
            public IntPtr pDevMode;
            public string pSepFile;
            public string pPrintProcessor;
            public string pDatatype;
            public string pParameters;
            public IntPtr pSecurityDescriptor;
            public uint Attributes;
            public uint Priority;
            public uint DefaultPriority;
            public uint StartTime;
            public uint UntilTime;
            public uint Status;
            public uint cJobs;
            public uint AveragePPM;
        }

        // Windows sets WORK_OFFLINE when a USB printer is unplugged or switched off.
        private const uint PRINTER_ATTRIBUTE_WORK_OFFLINE = 0x00000400;

        private static readonly (uint Flag, string Text)[] BlockingStatuses =
        {
            (0x00000080, "offline"),            // PRINTER_STATUS_OFFLINE
            (0x00001000, "not available"),      // PRINTER_STATUS_NOT_AVAILABLE
            (0x00000002, "in an error state"),  // PRINTER_STATUS_ERROR
            (0x00000010, "out of paper"),       // PRINTER_STATUS_PAPER_OUT
            (0x00000008, "jammed"),             // PRINTER_STATUS_PAPER_JAM
            (0x00400000, "open (door/cover)"),  // PRINTER_STATUS_DOOR_OPEN
            (0x00000001, "paused"),             // PRINTER_STATUS_PAUSED
        };

        /// <summary>
        /// The spooler accepts jobs for a disconnected printer and just queues them,
        /// so check the printer's state before submitting.
        /// </summary>
        private static string? GetPrinterNotReadyReason(IntPtr hPrinter, string printerName)
        {
            GetPrinter(hPrinter, 2, IntPtr.Zero, 0, out int needed);
            if (needed <= 0)
                return null; // state unknown; let the job go through

            IntPtr buffer = Marshal.AllocHGlobal(needed);
            try
            {
                if (!GetPrinter(hPrinter, 2, buffer, needed, out _))
                    return null;

                var info = Marshal.PtrToStructure<PRINTER_INFO_2>(buffer);

                if ((info.Attributes & PRINTER_ATTRIBUTE_WORK_OFFLINE) != 0)
                    return $"Printer '{printerName}' is offline. Check that it is switched on and the cable is connected.";

                foreach (var (flag, text) in BlockingStatuses)
                {
                    if ((info.Status & flag) != 0)
                        return $"Printer '{printerName}' is {text}.";
                }

                return null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public static bool SendZPLToPrinter(string printerName, string zplData, out string err)
        {
            err = null;

            IntPtr hPrinter;
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                err = $"Failed to open printer '{printerName}'. Check that it is installed with this exact name.";
                return false;
            }

            var notReady = GetPrinterNotReadyReason(hPrinter, printerName);
            if (notReady != null)
            {
                ClosePrinter(hPrinter);
                err = notReady;
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

            if (!success || dwWritten != zplData.Length)
            {
                err = "Printer did not accept the label data.";
                return false;
            }

            return true;
        }
    }
}
