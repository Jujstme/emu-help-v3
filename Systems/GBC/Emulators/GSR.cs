using System;
using System.Collections.Generic;
using System.Linq;
using EmuHelp.Logging;
using JHelper.Common.ProcessInterop;

namespace EmuHelp.Systems.GBC.Emulators;

internal class GSR : GBCEmulator
{
    private IntPtr sramPtr, wramPtr, iohramPtr;

    private readonly string[] sram_symbol_name = ["GSE_GB_SRAM_PTR", "GSR_GB_SRAM_PTR"];
    private readonly string[] wram_symbol_name = ["GSE_GB_WRAM_PTR", "GSR_GB_WRAM_PTR"];
    private readonly string[] iohram_symbol_name = ["GSE_GB_HRAM_PTR", "GSR_GB_HRAM_PTR"];

    internal GSR()
        : base()
    {
        Log.Info("  => Attached to emulator: GSR");
    }

    public override bool FindRAM(ProcessMemory process)
    {
        ProcessModule mainModule = process.MainModule;

        // We need to loop through the symbols to find the correct ones for SRAM, WRAM, and IOHRAM.
        // Instead of reading the symbols multiple times, we cache them locally. This avoid multiple redundant calls.
        List<Symbol> symbols = mainModule.Symbols.Where(s => sram_symbol_name.Contains(s.Name) || wram_symbol_name.Contains(s.Name) || iohram_symbol_name.Contains(s.Name)).ToList();

        sramPtr = symbols.FirstOrDefault(s => sram_symbol_name.Contains(s.Name)).Address;
        wramPtr = symbols.FirstOrDefault(s => wram_symbol_name.Contains(s.Name)).Address;
        iohramPtr = symbols.FirstOrDefault(s => iohram_symbol_name.Contains(s.Name)).Address;

        if (!process.ReadPointer(sramPtr, out IntPtr sram) || !process.ReadPointer(wramPtr, out IntPtr wram) || !process.ReadPointer(iohramPtr, out IntPtr iohram))
            return false;

        SRAM = sram;
        WRAM = wram;
        IOHRAM = iohram - 0x80;

        if (sram == IntPtr.Zero)
            Log.Info($"  => SRAM address unavailable at this moment, but it will be evaluated dynamically");
        else
            Log.Info($"  => SRAM address found at 0x{sram.ToString("X")}");

        if (wram == IntPtr.Zero)
            Log.Info($"  => WRAM address unavailable at this moment, but it will be evaluated dynamically");
        else
            Log.Info($"  => WRAM address found at 0x{wram.ToString("X")}");

        if (iohram == IntPtr.Zero)
            Log.Info($"  => IO_HRAM address unavailable at this moment, but it will be evaluated dynamically");
        else
            Log.Info($"  => IO_HRAM address found at 0x{IOHRAM.ToString("X")}");

        return true;
    }

    public override bool KeepAlive(ProcessMemory process)
    {
        if (!process.ReadPointer(sramPtr, out IntPtr sram)
            || !process.ReadPointer(wramPtr, out IntPtr wram)
            || !process.ReadPointer(iohramPtr, out IntPtr iohram))
            return false;

        SRAM = sram;
        WRAM = wram;
        IOHRAM = iohram - 0x80;

        return true;
    }
}