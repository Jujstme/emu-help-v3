using System;
using EmuHelp.Logging;
using JHelper.Common.MemoryUtils;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.Genesis.Emulators;

internal class SegaClassics : GenesisEmulator
{
    private IntPtr addr_base;

    internal SegaClassics()
        : base()
    {
        Endianness = Endianness.Little;
        Log.Info("  => Attached to emulator: SEGA Classics");
    }

    public override bool FindRAM(ProcessMemory _process)
    {
        bool isUsingGenesisWrapper = _process.Modules.TryGetValue("GenesisEmuWrapper.dll", out ProcessModule genesisWrapper);

        if (isUsingGenesisWrapper)
        {
            var target = new ScanPattern(2, "C7 05 ?? ?? ?? ?? ?? ?? ?? ?? A3 ?? ?? ?? ?? A3") { OnFound = _process.ReadPointer };
            IntPtr ptr = _process.Scan(target, genesisWrapper);
            
            if (ptr == IntPtr.Zero)
                return false;

            addr_base = ptr;
        }
        else
        {
            ScanPattern target = new ScanPattern(8, "89 2D ?? ?? ?? ?? 89 0D") { OnFound = _process.ReadPointer };
            IntPtr ptr = _process.Scan(target, _process.MainModule);

            if (ptr == IntPtr.Zero)
                return false;

            addr_base = ptr;
        }

        if (!_process.Read(addr_base, out IntPtr temp_addr))
            return false;

        RamBase = temp_addr;
        Log.Info($"  => RAM address found at 0x{RamBase.ToString("X")}");
        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        if (!process.Read(addr_base, out IntPtr ptr))
            return false;
        RamBase = ptr;
        return true;
    }
}