using EmuHelp.HelperBase;
using EmuHelp.Logging;
using EmuHelp.Systems.PSP;
using EmuHelp.Systems.PSP.Emulators;
using System;

public class Playstationportable : PSP { }

public class PlaystationPortable : PSP { }

public class PlayStationportable : PSP { }

public class PlayStationPortable : PSP { }

public class PSP : HelperBase
{
    private const uint MINSIZE = 0x08000000; // kernel memory base
    private const uint MAXSIZE = 0x40000000;

    private PSPEmulator? Pspemulator
    {
        get => (PSPEmulator?)emulatorClass;
        set => emulatorClass = value;
    }

    public PSP()
#if LIVESPLIT
        : this(true) { }

    public PSP(bool generateCode)
        : base(generateCode)
#else
        : base()
#endif
    {
        Log.Info("  => PSP Helper started");
    }

    internal override string[] ProcessNames { get; } =
    [
        "PPSSPPWindows64.exe",
        "PPSSPPWindows.exe",
        // "retroarch.exe",
    ];

    public override bool TryGetRealAddress(ulong address, out IntPtr realAddress)
    {
        realAddress = default;

        if (Pspemulator is null)
            return false;

        IntPtr baseRam = Pspemulator.RamBase;

        if (baseRam == IntPtr.Zero)
            return false;


        if (address >= MINSIZE && address < MAXSIZE)
        {
            realAddress = (IntPtr)((ulong)baseRam + address);
            return true;
        }
        return false;
    }

    internal override Emulator? AttachEmuClass()
    {
        if (emulatorProcess is null)
            return null;

        return emulatorProcess.ProcessName switch
        {
            "PPSSPPWindows64.exe" or "PPSSPPWindows.exe" => new Ppsspp(),
            // "retroarch.exe" => new Retroarch(),
            _ => null,
        };
    }
}