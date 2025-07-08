using System;
using System.Linq;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.PS1.Emulators;

#pragma warning disable IDE1006
internal class pSX : PS1Emulator
#pragma warning restore IDE1006
{
    internal pSX()
    : base()
    {
        Log.Info("  => Attached to emulator: pSX");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        ScanPattern.OnFoundCallback callback = _process.ReadPointer;

        ScanPattern[] patterns =
        [
            new(2, "8B 15 ?? ?? ?? ?? 8D 34 1A") { OnFound = callback },
            new(1, "A1 ?? ?? ?? ?? 8D 34 18") { OnFound = callback },
            new(1, "A1 ?? ?? ?? ?? 8B 7C 24 14") { OnFound = callback },
            new(1, "A1 ?? ?? ?? ?? 8B 6C 24") { OnFound = callback },
        ];

        IntPtr ptr = patterns
            .Select(pattern => _process.Scan(pattern, _process.MainModule.BaseAddress, _process.MainModule.ModuleMemorySize))
            .FirstOrDefault(addr => addr != IntPtr.Zero);

        if (ptr == IntPtr.Zero)
            return false;

        RamBase = ptr;

        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory _) => true;
}