using EmuHelp.HelperBase;
using EmuHelp.Systems.GCN;
using EmuHelp.Systems.GCN.Emulators;
using System;
using EmuHelp.Logging;
using JHelper.Common.MemoryUtils;

public class GameCube : GCN { }

public class Gamecube : GCN { }

public class GCN : HelperBase
{
    private const uint MINSIZE = 0x80000000;
    private const uint MAXSIZE = 0x81800000;

    private GCNEmulator? Gcnemulator
    {
        get => (GCNEmulator?)emulatorClass;
        set => emulatorClass = value;
    }

    public GCN()
#if LIVESPLIT
        : this(true) { }

    public GCN(bool generateCode)
        : base(generateCode)
#else
        : base()
#endif
    {
        Log.Info("  => GCN Helper started");
    }

    internal override string[] ProcessNames { get; } =
    [
        "Dolphin.exe",
        "retroarch.exe",
    ];

    public override bool TryGetRealAddress(ulong address, out IntPtr realAddress)
    {
        realAddress = default;

        if (Gcnemulator is null)
            return false;

        IntPtr baseRam = Gcnemulator.MEM1;

        if (baseRam == IntPtr.Zero)
            return false;


        if (address >= MINSIZE && address < MAXSIZE)
        {
            realAddress = (IntPtr)((ulong)baseRam + address - MINSIZE);
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
            "Dolphin.exe" => new Dolphin(),
            "retroarch.exe" => new Retroarch(),
            _ => null,
        };
    }

    public override bool TryRead<T>(out T value, ulong address, params int[] offsets)
    {
        if (emulatorProcess is null || Gcnemulator is null || !ResolvePath(out IntPtr realAddress, address, offsets))
        {
            value = default;
            return false;
        }

        if (!emulatorProcess.Read(realAddress, out value))
            return false;

        value = value.FromEndian(Gcnemulator.Endianness);
        return true;
    }

    protected override bool ResolvePath(out IntPtr finalAddress, ulong baseAddress, params int[] offsets)
    {
        // Check if the emulator process is valid and retrieve the real address for the base address
        if (emulatorProcess is null || Gcnemulator is null || !TryGetRealAddress(baseAddress, out finalAddress))
        {
            finalAddress = default;
            return false;
        }

        foreach (int offset in offsets)
        {
            if (!emulatorProcess.Read(finalAddress, out uint tempAddress)
                || !TryGetRealAddress((ulong)(tempAddress.FromEndian(Gcnemulator.Endianness) + offset), out finalAddress))
                return false;
        }

        return true;
    }

    public override unsafe bool TryReadArray<T>(out T[] value, uint size, ulong address, params int[] offsets)
    {
        if (emulatorProcess is null || Gcnemulator is null || !ResolvePath(out IntPtr realAddress, address, offsets))
        {
            value = new T[size];
            return false;
        }

        using (ArrayRental<T> buffer = (int)size * sizeof(T) <= 1024 ? new(stackalloc T[(int)size]) : new((int)size))
        {
            if (!emulatorProcess.ReadArray(realAddress, buffer.Span))
            {
                value = new T[(int)size];
                return false;
            }

            Endian.FromEndian(buffer.Span, Gcnemulator.Endianness);
            value = buffer.Span.ToArray();
        }

        return true;
    }
}