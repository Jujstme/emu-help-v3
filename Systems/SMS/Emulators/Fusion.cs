using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;
using System;

namespace EmuHelp.Systems.SMS.Emulators;

internal class Fusion : SMSEmulator
{
    internal Fusion()
        : base()
    {
        Log.Info("  => Attached to emulator: Fusion");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        RamBase = _process.Scan(new ScanPattern(4, "74 C8 83 3D") { OnFound = addr => _process.DerefOffsets(addr, 0, 0xC000) });
        if (RamBase == IntPtr.Zero)
            return false;

        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory process) => true;
}