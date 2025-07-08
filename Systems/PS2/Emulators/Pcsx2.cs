using System;
using System.Linq;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.PS2.Emulators;

internal class Pcsx2 : PS2Emulator
{
    private IntPtr addr_base;

    internal Pcsx2()
        : base()
    {
        Log.Info("  => Attached to emulator: PCSX2");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        if (_process.MainModule.Symbols.TryGetValue("EEmem", out IntPtr symbol))
        {
            addr_base = symbol;
        }
        else
        {
            if (_process.Is64Bit)
            {
                addr_base = _process.Scan(new ScanPattern(3, "48 8B ?? ?? ?? ?? ?? 25 F0 3F 00 00") { OnFound = addr => addr + 0x4 + _process.Read<int>(addr) });
                if (addr_base == IntPtr.Zero)
                    return false;
            }
            else
            {
                addr_base = new ScanPattern[]
                {
                new(2, "8B ?? ?? ?? ?? ?? 25 F0 3F 00 00") { OnFound = _process.ReadPointer },
                new(2, "8B ?? ?? ?? ?? ?? 81 ?? F0 3F 00 00") { OnFound = _process.ReadPointer },
                }
                .Select(_process.Scan).FirstOrDefault(addr => addr != IntPtr.Zero);

                if (addr_base == IntPtr.Zero)
                    return false;
            }
        }

        if (_process.Read(addr_base, out IntPtr ptr))
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
        RamBase = process.ReadPointer(addr_base);
        return true;
    }
}