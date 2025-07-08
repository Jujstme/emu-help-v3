using System;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.GBA.Emulators;

internal class VisualBoyAdvance : GBAEmulator
{
    private IntPtr ewram_pointer;
    private IntPtr iwram_pointer;

    internal VisualBoyAdvance()
        : base()
    {
        Log.Info("  => Attached to emulator: VisualBoyAdvance");
    }

    public override bool FindRAM(ProcessMemory process)
    {
        var mainModule = process.MainModule;

        if (process.Is64Bit)
        {
            ewram_pointer = process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? 81 E3 FF FF 03 00")
            { OnFound = addr => { IntPtr ptr = addr + 0x4 + process.Read<int>(addr); if (process.Read<byte>(addr + 10) == 0x48) ptr = process.ReadPointer(ptr); return ptr; } });
            if (ewram_pointer == IntPtr.Zero)
                return false;

            iwram_pointer = process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? 81 E3 FF 7F 00 00")
            { OnFound = addr => { var ptr = addr + 0x4 + process.Read<int>(addr); if (process.Read<byte>(addr + 10) == 0x48) ptr = process.ReadPointer(ptr); return ptr; } });
            if (iwram_pointer == IntPtr.Zero)
                return false;
        }
        else
        {
            ewram_pointer = process.Scan(new ScanPattern(1, "A1 ?? ?? ?? ?? 81 ?? FF FF 03 00") { OnFound = addr => process.ReadPointer(addr) });
            if (ewram_pointer == IntPtr.Zero)
                return false;

            if (ewram_pointer != IntPtr.Zero)
            {
                iwram_pointer = process.Scan(new ScanPattern(1, "A1 ?? ?? ?? ?? 81 ?? FF 7F 00 00") { OnFound = process.ReadPointer });
                if (iwram_pointer == IntPtr.Zero)
                    return false;
            }
            else
            {
                ewram_pointer = process.Scan(new ScanPattern(8, "81 E6 FF FF 03 00 8B 15") { OnFound = process.ReadPointer });
                if (ewram_pointer == IntPtr.Zero)
                    return false;
                iwram_pointer = ewram_pointer + 0x4;
            }
        }

        EWRAM = process.ReadPointer(ewram_pointer);
        IWRAM = process.ReadPointer(ewram_pointer);

        Log.Info(EWRAM != IntPtr.Zero ? $"  => EWRAM address found at 0x{EWRAM.ToString("X")}" : "  => EWRAM address currently not available: will be evaluated while emulator is running a game");
        Log.Info(IWRAM != IntPtr.Zero ? $"  => IWRAM address found at 0x{IWRAM.ToString("X")}" : "  => IWRAM address currently not available: will be evaluated while emulator is running a game");
        return true;
    }

    public override bool KeepAlive(ProcessMemory _process)
    {
        if (!_process.ReadPointer(ewram_pointer, out IntPtr ewram)
            || !_process.ReadPointer(iwram_pointer, out IntPtr iwram))
            return false;

        EWRAM = ewram;
        IWRAM = iwram;
        return true;
    }
}