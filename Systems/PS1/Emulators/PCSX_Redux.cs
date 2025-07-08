using System;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.PS1.Emulators;

internal class PCSXRedux : PS1Emulator
{
    private IntPtr baseAddress;
    private int[]? offsets;

    internal PCSXRedux()
        : base()
    {
        Log.Info("  => Attached to emulator: PCSX Redux");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        if (!_process.Is64Bit)
            return false;

        IntPtr addr = _process.Scan(new ScanPattern(3, "48 8B 05 ?? ?? ?? ?? 48 8B 80 ?? ?? ?? ?? 48 8B 50 ?? E8"));
        if (addr == IntPtr.Zero)
            return false;

        if (!_process.Read(addr, out int addri))
            return false;

        baseAddress = addr + 0x4 + addri;

        if (!_process.Read(addr + 7, out int offset1) || !_process.Read(addr + 14, out byte offset2))
            return false;
        offsets = [ offset1, offset2, 0 ];

        RamBase = _process.DerefOffsets(baseAddress, offsets);

        if (RamBase != IntPtr.Zero)
            Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        else
            Log.Info($"  => RAM address unavailable at this moment, but it will be evaluated dynamically");

        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        if (offsets is null)
            return false;

        RamBase = process.DerefOffsets(baseAddress, offsets);
        return true;
    }
}