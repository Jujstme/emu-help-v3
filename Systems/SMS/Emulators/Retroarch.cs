using System;
using System.Linq;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.SMS.Emulators;

internal class Retroarch : SMSEmulator
{
    private IntPtr core_base_address;

    private readonly string[] supportedCores =
    [
        "genesis_plus_gx_libretro.dll",
        "genesis_plus_gx_wide_libretro.dll",
        "picodrive_libretro.dll",
        "smsplus_libretro.dll",
        "gearsystem_libretro.dll",
    ];

    internal Retroarch()
        : base()
    {
        Log.Info("  => Attached to emulator: Retroarch");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        ProcessModule core = _process.Modules.FirstOrDefault(m => supportedCores.Contains(m.ModuleName));
        if (core == default)
            return false;

        core_base_address = core.BaseAddress;

        if (!core.Symbols.TryGetValue("retro_get_memory_data", out IntPtr baseScanAddr))
            return false;


        RamBase = _process.Is64Bit
            ? core.ModuleName switch
            {
                "genesis_plus_gx_libretro.dll" or "genesis_plus_gx_wide_libretro.dll" => _process.Scan(new ScanPattern(1, "05 ?? ?? ?? ?? C3") { OnFound = addr => _process.DerefOffsets(addr + 0x4 + _process.Read<int>(addr), 0x0) }, baseScanAddr, 0x100),
                "picodrive_libretro.dll" => _process.Scan(new ScanPattern(3, "48 8D 05") { OnFound = addr => addr + 0x4 + _process.Read<int>(addr) }, baseScanAddr, 0x100),
                "smsplus_libretro.dll" => _process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? 48 83 C0") { OnFound = addr => _process.DerefOffsets(addr + 0x4 + _process.Read<int>(addr), _process.Read<byte>(addr + 0x7)) }, baseScanAddr, 0x100),
                "gearsystem_libretro.dll" => _process.Scan(new ScanPattern(3, "48 8B 0D") { OnFound = addr => _process.DerefOffsets(addr + 0x4 + _process.Read<int>(addr), 0x0, 0x18, 0xC000) }, baseScanAddr, 0x100),
                _ => IntPtr.Zero,
            }
            : core.ModuleName switch
            {
                "genesis_plus_gx_libretro.dll" or "genesis_plus_gx_wide_libretro.dll" => _process.Scan(new ScanPattern(1, "B8 ?? ?? ?? ?? BA") { OnFound = _process.ReadPointer }, baseScanAddr, 0x100),
                "picodrive_libretro.dll" => _process.Scan(new ScanPattern(1, "BA ?? ?? ?? ?? B8") { OnFound = _process.ReadPointer }, baseScanAddr, 0x100),
                "smsplus_libretro.dll" => _process.Scan(new ScanPattern(1, "B8 ?? ?? ?? ?? B9 00 00 00 00") { OnFound = _process.ReadPointer }, baseScanAddr, 0x100),
                "gearsystem_libretro.dll" => _process.Scan(new ScanPattern(2, "8B 0D ?? ?? ?? ?? E8") { OnFound = addr => _process.DerefOffsets(addr, 0x0, 0x0, 0xC, 0xC000) }, baseScanAddr, 0x100),
                _ => IntPtr.Zero,
            };

        if (RamBase == IntPtr.Zero)
            return false;

        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        return process.Read<byte>(core_base_address, out _);
    }
}