using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;
using System;

namespace EmuHelp.Systems.PS1.Emulators;

internal class Duckstation : PS1Emulator
{
    private IntPtr ramPointer = IntPtr.Zero;

    internal Duckstation()
        : base()
    {
        Log.Info("  => Attached to emulator: Duckstation");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        if (!_process.Is64Bit)
            return false;

        // Evaluate and calculate the value of the main pointer to WRAM
        if (_process.MainModule.Symbols.TryGetValue("RAM", out IntPtr symbol))
        {
            ramPointer = symbol;
        }
        // Old Duckstation releases don't use debug symbols. In this case, we employ a dedicated signature scan
        else
        {
            ramPointer = _process.MainModule.Scan(new ScanPattern(3, "48 89 0D ?? ?? ?? ?? B8") { OnFound = addr => addr + 0x4 + _process.Read<int>(addr) });
            if (ramPointer == IntPtr.Zero)
                return false;
        }

        if (_process.Read(ramPointer, out IntPtr ptr))
        {
            RamBase = ptr;
            Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        }
        else
        {
            RamBase = IntPtr.Zero;
            Log.Info($"  => RAM address unavailable at this moment, but it will be evaluated dynamically");
        }

        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        RamBase = process.ReadPointer(ramPointer);
        return true;
    }
}