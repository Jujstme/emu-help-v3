using System;
using System.Linq;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.PS1.Emulators;

internal class Retroarch : PS1Emulator
{
    private IntPtr core_base_address;

    private readonly string[] supportedCores =
    [
        "mednafen_psx_hw_libretro.dll",
        "mednafen_psx_libretro.dll",
        "swanstation_libretro.dll",
        "pcsx_rearmed_libretro.dll",
    ];

    internal Retroarch()
        : base()
    {
        Log.Info("  => Attached to emulator: Retroarch");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        ProcessModule currentCore = _process.Modules.FirstOrDefault(m => supportedCores.Contains(m.ModuleName));

        if (currentCore == default)
            return false;

        core_base_address = currentCore.BaseAddress;

        if (!currentCore.Symbols.TryGetValue("retro_get_memory_data", out IntPtr baseScanAddr))
            return false;

        IntPtr ptr = _process.Is64Bit
            ? currentCore.ModuleName switch
            {
                "mednafen_psx_hw_libretro.dll" or "mednafen_psx_libretro.dll" => _process.Scan(new ScanPattern(4, "48 0F 44 05") { OnFound = addr => _process.DerefOffsets(addr + 0x4 + _process.Read<int>(addr), 0) }, baseScanAddr, 0x100),
                "swanstation_libretro.dll" => _process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? C3") { OnFound = addr => _process.DerefOffsets(addr + 0x4 + _process.Read<int>(addr), 0) }, baseScanAddr, 0x100),
                "pcsx_rearmed_libretro.dll" => _process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? 48 8B 00") { OnFound = addr => _process.DerefOffsets(addr + 0x4 + _process.Read<int>(addr), 0, 0) }, baseScanAddr, 0x100),
                _ => IntPtr.Zero,
            }
            : currentCore.ModuleName switch
            {
                "mednafen_psx_hw_libretro.dll" or "mednafen_psx_libretro.dll" => _process.Scan(new ScanPattern(3, "0F 44 05") { OnFound = addr => { return _process.DerefOffsets(addr, 0, 0); } }, baseScanAddr, 0x100),
                "swanstation_libretro.dll" => _process.Scan(new ScanPattern(3, "74 ?? A1") { OnFound = addr => _process.DerefOffsets(addr, 0, 0) }, baseScanAddr, 0x100),
                "pcsx_rearmed_libretro.dll" => _process.Scan(new ScanPattern(3, "0F 44 05 ?? ?? ?? ?? C3") { OnFound = addr => _process.DerefOffsets(addr, 0, 0) }, baseScanAddr, 0x100),
                _ => IntPtr.Zero,
            };

        if (ptr == IntPtr.Zero)
            return false;

        RamBase = ptr;

        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        return process.Read(core_base_address, out byte _);
    }
}