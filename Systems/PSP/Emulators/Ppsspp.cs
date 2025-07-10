using System;
using System.Runtime.InteropServices;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.PSP.Emulators;

internal class Ppsspp : PSPEmulator
{
    private IntPtr addr_base;

    internal Ppsspp()
        : base()
    {
        Log.Info("  => Attached to emulator: PPSSPP");
    }

    public override bool FindRAM(ProcessMemory process)
    {
        // Possible signatures usable as alternative to FindWindow
        // 64-bit: 48 8B 05 ?? ?? ?? ?? 44 8B 34 01
        // 32-bit: A1 ?? ?? ?? ?? 8B 04 06

        IntPtr hwnd = FindWindow("PPSSPPWnd", null);
        if (hwnd == IntPtr.Zero)
            return false;

        // https://www.ppsspp.org/docs/reference/process-hacks/
        nint address = (nint)SendMessage(hwnd, 0xB118, 0, 2);

        if (process.Is64Bit)
            address += (nint)SendMessage(hwnd, 0xB118, 0, 3) << 32;

        if (address == 0)
            return false;

        addr_base = address;

        RamBase = process.ReadPointer(addr_base);

        if (RamBase == IntPtr.Zero)
            Log.Info($"  => RAM address unavailable at this moment, but it will be evaluated dynamically");
        else
            Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        
        return true;

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        static extern uint SendMessage(nint hWnd, int wMsg, nint wParam, nint lParam);
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        RamBase = process.ReadPointer(addr_base);
        return true;
    }
}
