using System;
using EmuHelp.Logging;
using JHelper.Common.MemoryUtils;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.Genesis.Emulators;

internal class Fusion : GenesisEmulator
{
    private IntPtr addr_base;

    internal Fusion()
        : base()
    {
        Endianness = Endianness.Big;
        Log.Info("  => Attached to emulator: Fusion");
    }

    public override bool FindRAM(ProcessMemory process)
    {
        IntPtr ptr = process.MainModule.Scan(new ScanPattern(1, "75 2F 6A 01") { OnFound = addr => addr + 0x3 + process.Read<byte>(addr) });

        if (ptr == IntPtr.Zero)
            return false;

        if (!process.ReadPointer(ptr, out addr_base) || addr_base == IntPtr.Zero)
            return false;

        RamBase = process.ReadPointer(addr_base);

        if (RamBase != IntPtr.Zero)
            Log.Info($"  => WRAM address found at 0x{RamBase.ToString("X")}");
        else
            Log.Info($"  => WRAM address will be evaluated while a game is running");

        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        bool success = process.ReadPointer(addr_base, out IntPtr ptr);
        if (success)
            RamBase = ptr;
        return success;
    }
}