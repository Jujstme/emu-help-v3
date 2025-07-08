using System;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.SMS.Emulators;

internal class Mednafen : SMSEmulator
{
    internal Mednafen()
        : base()
    {
        Log.Info("  => Attached to emulator: Mednafen");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        RamBase = _process.Scan(_process.Is64Bit
            ? new ScanPattern(7, "25 FF 1F 00 00 88 90") { OnFound = addr => (IntPtr)_process.Read<int>(addr) }
            : new ScanPattern(8, "25 FF 1F 00 00 0F B6 80") { OnFound = _process.ReadPointer });

        if (RamBase == IntPtr.Zero)
            return false;

        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory _) => true;
}