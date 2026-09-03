using EmuHelp.HelperBase;
using EmuHelp.Logging;
using EmuHelp.Systems.Wii;
using EmuHelp.Systems.Wii.Emulators;
using JHelper.Common.MemoryUtils;
using System;

public class Wii : WII { }

public class WII : HelperBase
{
    private const uint MEM1_MINSIZE = 0x80000000;
    private const uint MEM1_MAXSIZE = 0x81800000;
    private const uint MEM2_MINSIZE = 0x90000000;
    private const uint MEM2_MAXSIZE = 0x94000000;

    private WIIEmulator? Wiiemulator
    {
        get => (WIIEmulator?)emulatorClass;
        set => emulatorClass = value;
    }

    public WII()
#if LIVESPLIT
        : this(true) { }

    public WII(bool generateCode)
        : base(generateCode)
#else
        : base()
#endif
    {
        Log.Info("  => Wii Helper started");
    }

    internal override string[] ProcessNames { get; } =
    [
        "Dolphin.exe",
        "retroarch.exe",
    ];

    public override bool TryGetRealAddress(ulong address, out IntPtr realAddress)
    {
        if (Wiiemulator is null)
        {
            realAddress = default;
            return false;
        }

        if (address >= MEM1_MINSIZE && address < MEM1_MAXSIZE)
        {
            realAddress = (IntPtr)((ulong)Wiiemulator.MEM1 + address - MEM1_MINSIZE);
            return true;
        }
        else if (address >= MEM2_MINSIZE && address < MEM2_MAXSIZE)
        {
            realAddress = (IntPtr)((ulong)Wiiemulator.MEM2 + address - MEM2_MINSIZE);
            return true;
        }
        else
        {
            realAddress = default;
            return false;
        }
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
        if (emulatorProcess is null || Wiiemulator is null || !ResolvePath(out IntPtr realAddress, address, offsets))
        {
            value = default;
            return false;
        }

        if (!emulatorProcess.Read(realAddress, out value))
            return false;

        value = value.FromEndian(Wiiemulator.Endianness);
        return true;
    }

    protected override bool ResolvePath(out IntPtr finalAddress, ulong baseAddress, params int[] offsets)
    {
        // Check if the emulator process is valid and retrieve the real address for the base address
        if (emulatorProcess is null || Wiiemulator is null || !TryGetRealAddress(baseAddress, out finalAddress))
        {
            finalAddress = default;
            return false;
        }

        foreach (int offset in offsets)
        {
            if (!emulatorProcess.Read(finalAddress, out uint tempAddress)
                || !TryGetRealAddress((ulong)(tempAddress.FromEndian(Wiiemulator.Endianness) + offset), out finalAddress))
                return false;
        }

        return true;
    }

    public override unsafe bool TryReadArray<T>(out T[] value, uint size, ulong address, params int[] offsets)
    {
        if (emulatorProcess is null || Wiiemulator is null || !ResolvePath(out IntPtr realAddress, address, offsets))
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
            
            Endian.FromEndian(buffer.Span, Wiiemulator.Endianness);
            value = buffer.Span.ToArray();
        }

        return true;
    }
}