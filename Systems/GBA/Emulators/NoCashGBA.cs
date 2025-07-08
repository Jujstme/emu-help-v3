using System;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.GBA.Emulators;

internal class NoCashGBA : GBAEmulator
{
    private IntPtr baseAddress;

    private const int EWRAM_OFFSET = 0x938C + 0x8;
    private const int IWRAM_OFFSET = 0x95D4;

    internal NoCashGBA()
        : base()
    {
        Log.Info("  => Attached to emulator: NO$GBA");
    }

    public override bool FindRAM(ProcessMemory process)
    {
        var mainModule = process.MainModule;

        baseAddress = process.Scan(new ScanPattern(2, "FF 35 ?? ?? ?? ?? 55") { OnFound = addr => process.ReadPointer(addr) });
        if (baseAddress == IntPtr.Zero)
            return false;

        IntPtr addr = process.ReadPointer(baseAddress);

        EWRAM = process.ReadPointer(addr + EWRAM_OFFSET);
        IWRAM = process.ReadPointer(addr + IWRAM_OFFSET);

        Log.Info($"  => EWRAM address found at 0x{EWRAM.ToString("X")}");
        Log.Info($"  => IWRAM address found at 0x{IWRAM.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        if (!process.ReadPointer(baseAddress, out IntPtr addr))
            return false;

        if (!process.ReadPointer(addr + EWRAM_OFFSET, out IntPtr ewram)
            || !process.ReadPointer(addr + IWRAM_OFFSET, out IntPtr iwram))
            return false;

        EWRAM = ewram;
        IWRAM = iwram;

        return true;
    }
}