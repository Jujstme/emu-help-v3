using System;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.PS1.Emulators;

#pragma warning disable IDE1006
internal class ePSXe : PS1Emulator
#pragma warning restore IDE1006
{
    internal ePSXe()
        : base()
    {
        Log.Info("  => Attached to emulator: ePSXe");
    }

    public override bool FindRAM(ProcessMemory process)
    {
        IntPtr ptr = process.MainModule.Scan(new ScanPattern(5, "C1 E1 10 8D 89") { OnFound = process.ReadPointer });
        
        if (ptr == IntPtr.Zero)
            return false;
        
        RamBase = ptr;

        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory _) => true;
}