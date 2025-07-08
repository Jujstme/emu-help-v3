using System;
using System.Linq;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.GBC.Emulators;

internal class GSR_qt : GBCEmulator
{
    private IntPtr base_addr;

    internal GSR_qt()
        : base()
    {
        Log.Info("  => Attached to emulator: GSR_qt");
    }

    public override bool FindRAM(ProcessMemory process)
    {
        ScanPattern target = new(0, "20 ?? ?? ?? 20 ?? ?? ?? 20 ?? ?? ?? 20 ?? ?? ?? 05 00 00");

        IntPtr ptr = process
            .MemoryPages
            .Where(p => p.RegionSize == 0xFF000)
            .Select(p => process.Scan(target, p.BaseAddress, (int)p.RegionSize))
            .FirstOrDefault(addr => addr != IntPtr.Zero);

        if (ptr == IntPtr.Zero)
            return false;

        base_addr = ptr - 0x10;

        if (!process.Read(base_addr, out int wram))
            return false;

        WRAM = (IntPtr)wram;
        IOHRAM = ptr + 0x13FC;


        Log.Info($"  => WRAM address found at 0x{WRAM.ToString("X")}");
        Log.Info($"  => IO_HRAM address found at 0x{IOHRAM.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        if (!process.Read<int>(base_addr, out int wram))
            return false;

        WRAM = (IntPtr)wram;
        return true;
    }
}