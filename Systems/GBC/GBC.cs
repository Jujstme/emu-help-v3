using EmuHelp.HelperBase;
using EmuHelp.Logging;
using EmuHelp.Systems.GBC;
using EmuHelp.Systems.GBC.Emulators;
using System;

public class GameBoyColor : GBC { }

public class Gameboycolor : GBC { }

public class GameboyColor : GBC { }

public class GBC : HelperBase
{
    // GameBoy Color memory layout is descibed here: https://gbdev.io/pandocs/Memory_Map.html
    // We are mostly interested in these:
    // A000 - BFFF  8 KiB External RAM From cartridge (SRAM)
    // C000 - DFFF  8 KiB Work RAM (WRAM)
    // FF00 - FF7F  I/O Registers
    // FF80 - FFFE  High RAM(HRAM)

    private const uint SRAM_MIN = 0xA000, SRAM_MAX = 0xBFFF;
    private const uint WRAM_MIN = 0xC000, WRAM_MAX = 0xDFFF;
    private const uint IOHRAM_MIN = 0xFF00, IOHRAM_MAX = 0x10000;

    private GBCEmulator? emulator
    {
        get => (GBCEmulator?)emulatorClass;
        set => emulatorClass = value;
    }

    public GBC()
#if LIVESPLIT
        : this(true) { }

    public GBC(bool generateCode)
        : base(generateCode)
#else
        : base()
#endif
    {
        Log.Info("  => GBC Helper started");
    }

    internal override string[] ProcessNames { get; } =
    {
        "GSR.exe",
        "GSE.exe",
        "gambatte_speedrun.exe"
    };

    public override bool TryGetRealAddress(ulong address, out IntPtr realAddress)
    {
        realAddress = default;

        if (emulator is null)
        {
            return false;
        }
        else if (address >= SRAM_MIN && address <= SRAM_MAX)
        {
            realAddress = (IntPtr)((ulong)emulator.SRAM + address - SRAM_MIN);
            return true;
        }
        else if (address >= WRAM_MIN && address <= WRAM_MAX)
        {
            realAddress = (IntPtr)((ulong)emulator.WRAM + address - WRAM_MIN);
            return true;
        }
        else if (address >= IOHRAM_MIN && address <= IOHRAM_MAX)
        {
            realAddress = (IntPtr)((ulong)emulator.IOHRAM + address - IOHRAM_MIN);
            return true;
        }
        else
        {
            return false;
        }
    }

    internal override Emulator? AttachEmuClass()
    {
        if (emulatorProcess is null)
            return null;

        return emulatorProcess.ProcessName switch
        {
            "GSR.exe" or "GSE.exe" => new GSR(),
            "gambatte_speedrun.exe" => new GSR_qt(),
            _ => null,
        };
    }
}