using System;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.PSP.Emulators;

internal class Ppsspp : PSPEmulator
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]    
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

    private static readonly string ClassName = "PPSSPPWnd";

    private IntPtr addr_base;

    internal Ppsspp()
        : base()
    {
        Log.Info("  => Attached to emulator: PPSSPP");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        IntPtr hwnd = FindWindow(ClassName, null);
        if (hwnd != IntPtr.Zero)
        {
            // https://www.ppsspp.org/docs/reference/process-hacks/
            int lower = SendMessage(hwnd, 0xB118, 0, 0);
            if (_process.Is64Bit)
            {
                int upper = SendMessage(hwnd, 0xB118, 0, 1);
                addr_base = (IntPtr)((upper << 32) + lower);
            }
            else
            {
                addr_base = (IntPtr)lower;
            }

            if (addr_base == IntPtr.Zero)
                return false;

            if (!_process.Read(addr_base, out IntPtr ptr))
                return false;
            RamBase = ptr;
        }

        if (RamBase != IntPtr.Zero)
                Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        if (!process.Read(addr_base, out IntPtr ptr))
            return false;
        RamBase = ptr;

        return true;
    }
}