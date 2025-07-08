using System;
using EmuHelp.Logging;
using JHelper.Common.MemoryUtils;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.Genesis.Emulators;

internal class Gens : GenesisEmulator
{
    internal Gens()
        : base()
    {
        Log.Info("  => Attached to emulator: Gens");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        IntPtr ptr = _process.Scan(new ScanPattern(11, "72 ?? 81 ?? FF FF 00 00 66 8B"));
        if (ptr == IntPtr.Zero)
            return false;

        if (!_process.Read(ptr, out byte endianByte))
            return false;

        Endianness = endianByte == 0x86 ? Endianness.Big : Endianness.Little;

        if (!_process.Read(ptr, out int intptr))
            return false;

        RamBase = (IntPtr)intptr;

        Log.Info($"  => WRAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory _)
    {
        return true;
    }
}