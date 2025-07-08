using System;
using System.Linq;
using EmuHelp.Logging;
using JHelper.Common.MemoryUtils;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.Genesis.Emulators;

internal class Retroarch : GenesisEmulator
{
    private IntPtr core_base_address;
    private readonly string[] supportedCores =
    [
        "blastem_libretro.dll",
        "genesis_plus_gx_libretro.dll",
        "genesis_plus_gx_wide_libretro.dll",
        "picodrive_libretro.dll",
    ];

    internal Retroarch()
        : base()
    {
        Endianness = Endianness.Little;
        Log.Info("  => Attached to emulator: Retroarch");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        ProcessModule core = _process.Modules.FirstOrDefault(m => supportedCores.Contains(m.ModuleName));
        
        if (core == default)
            return false;

        core_base_address = core.BaseAddress;

        if (core.ModuleName == "blastem_libretro.dll")
        {
            ScanPattern target = new ScanPattern(11, "72 0E 81 E1 FF FF 00 00 66 8B 89 ?? ?? ?? ?? C3") { OnFound = addr => (IntPtr)_process.Read<int>(addr) };

            IntPtr ptr = _process.MemoryPages.Where(p => p.RegionSize == 0x101000 && (p.Protect & MemoryProtection.PAGE_EXECUTE_READWRITE) != 0)
                .Select(page => _process.Scan(target, page.BaseAddress, (int)page.RegionSize))
                .FirstOrDefault(addr => addr != IntPtr.Zero);

            if (ptr == IntPtr.Zero)
                return false;

            RamBase = ptr;
        }
        else
        {
            if (!core.Symbols.TryGetValue("retro_get_memory_data", out IntPtr baseScanAddr))
                return false;

            IntPtr ptr = _process.Is64Bit
                ? core.ModuleName switch
                {
                    "genesis_plus_gx_libretro.dll" or "genesis_plus_gx_wide_libretro.dll" => _process.Scan(new ScanPattern(1, "05 ?? ?? ?? ?? C3") { OnFound = addr => _process.DerefOffsets(addr + 0x4 + _process.Read<int>(addr), 0x0) }, baseScanAddr, 0x100),
                    "picodrive_libretro.dll" => _process.Scan(new ScanPattern(3, "48 8D 05") { OnFound = addr => addr + 0x4 + _process.Read<int>(addr) }, baseScanAddr, 0x100),
                    _ => IntPtr.Zero,
                }
                : core.ModuleName switch
                {
                    "genesis_plus_gx_libretro.dll" or "genesis_plus_gx_wide_libretro.dll" => _process.Scan(new ScanPattern(1, "B8 ?? ?? ?? ?? BA") { OnFound = _process.ReadPointer }, baseScanAddr, 0x100),
                    "picodrive_libretro.dll" => _process.Scan(new ScanPattern(1, "BA ?? ?? ?? ?? B8") { OnFound = _process.ReadPointer }, baseScanAddr, 0x100),
                    _ => IntPtr.Zero,
                };

            if (ptr == IntPtr.Zero)
                return false;

            RamBase = ptr;
        }


        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory process) => process.Read(core_base_address, out byte _);
}