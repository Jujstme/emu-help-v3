using System;
using System.Linq;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.GBA.Emulators;

internal class Retroarch : GBAEmulator
{
    private IntPtr core_base_address;
    
    private readonly string[] supportedCores =
    [
        "vbam_libretro.dll",
        "mednafen_gba_libretro.dll",
        "vba_next_libretro.dll",
        "mgba_libretro.dll",
        "gpsp_libretro.dll",
    ];

    internal Retroarch()
        : base()
    {
        Log.Info("  => Attached to emulator: Retroarch");
    }

    public override bool FindRAM(ProcessMemory process)
    {
        ProcessModule core = process.Modules.FirstOrDefault(m => supportedCores.Contains(m.ModuleName));

        if (core == default)
            return false;

        core_base_address = core.BaseAddress;
        IntPtr iwram, ewram;
        bool success = core.ModuleName switch
        {
            "vbam_libretro.dll" or "mednafen_gba_libretro.dll" or "vba_next_libretro.dll" => vba(process, core, out ewram, out iwram),
            "mgba_libretro.dll" => mGBA(process, out ewram, out iwram),
            "gpsp_libretro.dll" => gpSP(process, core, out ewram, out iwram),
            _ => throw new NotImplementedException(),
        };

        if (!success)
            return false;

        EWRAM = ewram;
        IWRAM = iwram;

        Log.Info($"  => EWRAM address found at 0x{EWRAM.ToString("X")}");
        Log.Info($"  => IWRAM address found at 0x{IWRAM.ToString("X")}");
        return true;
    }

    private bool vba(ProcessMemory process, ProcessModule currentCore, out IntPtr ewram, out IntPtr iwram)
    {
        IntPtr ptr, ewram_pointer, iwram_pointer;
        ewram = IntPtr.Zero;
        iwram = IntPtr.Zero;

        if (process.Is64Bit)
        {
            ptr = process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? 81 E1 FF FF 03 00") { OnFound = addr => { IntPtr ptr = addr + 0x4 + process.Read<int>(addr); if (process.Read<byte>(addr + 10) == 0x48) ptr = process.ReadPointer(ptr); return ptr; }}, currentCore);
            if (ptr == IntPtr.Zero)
                return false;
            ewram_pointer = ptr;

            ptr = process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? 81 E1 FF 7F 00 00") { OnFound = addr => { IntPtr ptr = addr + 0x4 + process.Read<int>(addr); if (process.Read<byte>(addr + 10) == 0x48) ptr = process.ReadPointer(ptr); return ptr; }}, currentCore);
            if (ptr == IntPtr.Zero)
                return false;
            iwram_pointer = ptr;
        }
        else
        {
            ptr = process.Scan(new ScanPattern(1, "A1 ?? ?? ?? ?? 81 ?? FF FF 03 00") { OnFound = addr => process.ReadPointer(addr) }, currentCore);
            if (ptr == IntPtr.Zero)
                return false;
            ewram_pointer = ptr;

            ptr = process.Scan(new ScanPattern(1, "A1 ?? ?? ?? ?? 81 ?? FF 7F 00 00") { OnFound = addr => process.ReadPointer(addr) }, currentCore);
            if (ptr == IntPtr.Zero)
                return false;
            iwram_pointer = ptr;
        }

        return process.ReadPointer(ewram_pointer, out ewram) && process.ReadPointer(iwram_pointer, out iwram);
    }

    private bool mGBA(ProcessMemory process, out IntPtr ewram, out IntPtr iwram)
    {
        ewram = process.MemoryPages
            .FirstOrDefault(p => (int)p.RegionSize == 0x48000
            && p.Type == MemoryType.MEM_PRIVATE
            && p.State == MemoryState.MEM_COMMIT
            && p.Protect == MemoryProtection.PAGE_READWRITE)
            .BaseAddress;

        iwram = ewram == IntPtr.Zero ? IntPtr.Zero : ewram + 0x40000;
        return ewram != IntPtr.Zero;
    }

    private bool gpSP(ProcessMemory process, ProcessModule currentCore, out IntPtr ewram, out IntPtr iwram)
    {
        ewram = IntPtr.Zero;
        iwram = IntPtr.Zero;


        IntPtr ptr = process.Is64Bit
            ? process.Scan(new ScanPattern(3, "48 8B 15 ?? ?? ?? ?? 8B 42 40") { OnFound = addr => process.ReadPointer(addr + 0x4 + process.Read<int>(addr)) }, currentCore)
            : process.Scan(new ScanPattern(1, "A3 ?? ?? ?? ?? F7 C5 02 00 00 00") { OnFound = process.ReadPointer }, currentCore);

        ewram = process.Scan(new ScanPattern(8, "25 FF FF 03 00 88 94 03") { OnFound = addr => ptr + process.Read<int>(addr) }, currentCore);
        if (ewram == IntPtr.Zero)
            return false;
        
        iwram = process.Scan(new ScanPattern(9, "25 FE 7F 00 00 66 89 94 03") { OnFound = addr => ptr + process.Read<int>(addr) }, currentCore);
        if (iwram == IntPtr.Zero)
            return false;

        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        return process.Read(core_base_address, out byte _);
    }
}