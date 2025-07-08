using System;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.PS1.Emulators;

internal class Xebra : PS1Emulator
{
    internal Xebra()
        : base()
    {
        Log.Info("  => Attached to emulator: Xebra");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        IntPtr ptr = _process.Scan(new ScanPattern(1, "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 89 C8 C1 F8 10") { OnFound = addr => addr + 0x4 + _process.Read<int>(addr) });

        if (ptr == IntPtr.Zero)
            return false;

        if (!_process.Read(ptr + 0x16A, out IntPtr ptrl) || !_process.Read(ptrl, out ptrl))
            return false;
        RamBase = ptrl;

        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory _) => true;
}