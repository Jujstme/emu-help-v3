using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;
using System;

namespace EmuHelp.Systems.PS1.Emulators;

internal class Mednafen : PS1Emulator
{
    internal Mednafen()
        : base()
    {
        Log.Info("  => Attached to emulator: Mednafen");
    }

    public override bool FindRAM(ProcessMemory _process)
    {

        IntPtr ptr = _process.Scan(_process.Is64Bit
            ? new ScanPattern(5, "89 01 0F B6 82") { OnFound = addr => (IntPtr)_process.Read<int>(addr) }
            : new ScanPattern(5, "89 01 0F B6 82 ?? ?? ?? ?? C3") { OnFound = _process.ReadPointer });

        if (ptr == IntPtr.Zero)
            return false;

        RamBase = ptr;

        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory _) => true;
}