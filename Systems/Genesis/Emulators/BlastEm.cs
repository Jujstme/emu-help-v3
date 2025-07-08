using System;
using System.Linq;
using EmuHelp.Logging;
using JHelper.Common.MemoryUtils;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.Genesis.Emulators;

internal class BlastEm : GenesisEmulator
{
    internal BlastEm()
        : base()
    {
        Endianness = Endianness.Little;
        Log.Info("  => Attached to emulator: BlastEm");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        ScanPattern target = new ScanPattern(11, "72 0E 81 E1 FF FF 00 00 66 8B 89 ?? ?? ?? ?? C3") { OnFound = _process.ReadPointer };

        IntPtr ptr = _process.MemoryPages.Where(p => p.RegionSize == 0x101000 && (p.Protect & MemoryProtection.PAGE_EXECUTE_READWRITE) != 0)
            .Select(page => _process.Scan(target, page.BaseAddress, (int)page.RegionSize))
            .FirstOrDefault(addr => addr != IntPtr.Zero);

        if (ptr == IntPtr.Zero)
            return false;

        RamBase = ptr;

        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory _) => true;
}