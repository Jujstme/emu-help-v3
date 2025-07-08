using System;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.GBA.Emulators;

internal class Mednafen : GBAEmulator
{
    private IntPtr ewram_pointer;
    private IntPtr iwram_pointer;

    internal Mednafen()
        : base()
    {
        Log.Info("  => Attached to emulator: Mednafen");
    }

    public override bool FindRAM(ProcessMemory process)
    {
        ProcessModule mainModule = process.MainModule;
        bool is64bit = process.Is64Bit;

        ewram_pointer = is64bit
            ? process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? 81 E1 FF FF 03 00")
            { OnFound = (addr) => { var ptr = addr + 0x4 + process.Read<int>(addr); if (process.Read<byte>(addr + 10) == 0x48) ptr = process.ReadPointer(ptr); return ptr; } }, mainModule)
            : process.Scan(new ScanPattern(1, "A1 ?? ?? ?? ?? 81 ?? FF FF 03 00") { OnFound = addr => process.ReadPointer(addr) }, mainModule);
        if (ewram_pointer == IntPtr.Zero)
            return false;
        if (!process.ReadPointer(ewram_pointer, out IntPtr ewram))
            return false;


        iwram_pointer = is64bit
            ? process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? 81 E1 FF 7F 00 00")
            { OnFound = addr => { var ptr = addr + 0x4 + process.Read<int>(addr); if (process.Read<byte>(addr + 10) == 0x48) ptr = process.ReadPointer(ptr); return ptr; } })
            : process.Scan(new ScanPattern(1, "A1 ?? ?? ?? ?? 81 ?? FF 7F 00 00") { OnFound = addr => process.ReadPointer(addr) });
        if (iwram_pointer == IntPtr.Zero)
            return false;
        if (!process.ReadPointer(iwram_pointer, out IntPtr iwram))
            return false;

        EWRAM = ewram;
        IWRAM = iwram;

        Log.Info("  => Hooked to emulator: Mednafen");
        Log.Info($"  => EWRAM address found at 0x{EWRAM.ToString("X")}");
        Log.Info($"  => IWRAM address found at 0x{IWRAM.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory _process)
    {
        EWRAM = _process.ReadPointer(ewram_pointer);
        if (EWRAM == IntPtr.Zero)
            return false;
        
        IWRAM = _process.ReadPointer(iwram_pointer);
        if (IWRAM == IntPtr.Zero)
            return false;

        return true;
    }
}