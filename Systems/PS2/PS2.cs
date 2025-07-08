using EmuHelp.HelperBase;
using EmuHelp.Logging;
using EmuHelp.Systems.PS2;
using EmuHelp.Systems.PS2.Emulators;
using System;

public class Playstation2 : PS2 { }

public class PlayStation2 : PS2 { }

public class PS2 : HelperBase
{
    private const uint MINSIZE = 0x00100000;
    private const uint MAXSIZE = 0x02000000;

    private PS2Emulator? Ps2emulator
    {
        get => (PS2Emulator?)emulatorClass;
        set => emulatorClass = value;
    }

    public PS2()
#if LIVESPLIT
        : this(true) { }

    public PS2(bool generateCode)
        : base(generateCode)
#else
        : base()
#endif
    {
        Log.Info("  => PS2 Helper started");
    }

    internal override string[] ProcessNames { get; } =
    [
        "pcsx2x64.exe",
        "pcsx2-qt.exe",
        "pcsx2x64-avx2.exe",
        "pcsx2-avx2.exe",
        "pcsx2.exe",
        "retroarch.exe",
    ];

    public override bool TryGetRealAddress(ulong address, out IntPtr realAddress)
    {
        realAddress = default;

        if (Ps2emulator is null)
            return false;

        IntPtr baseRam = Ps2emulator.RamBase;

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
            "pcsx2x64.exe" or "pcsx2-qt.exe" or "pcsx2x64-avx2.exe" or "pcsx2-avx2.exe" or "pcsx2.exe" => new Pcsx2(),
            "retroarch.exe" => new Retroarch(),
            _ => null,
        };
    }
}