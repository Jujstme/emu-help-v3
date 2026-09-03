using EmuHelp.HelperBase;
using EmuHelp.Logging;
using EmuHelp.Systems.Xbox360;
using EmuHelp.Systems.Xbox360.Emulators;
using JHelper.Common.MemoryUtils;
using System;

public class X360 : Xbox360 { }

public class Xbox360 : HelperBase
{
    // Xbox 360s Memory Regions
    // 0x30000000 - Heap Memory Region
    // 0x40000000 - Allocated Data Memory Region
    // 0x70000000 - the Stack Memory Region
    // 0x82000000 - Basefiles Memory Region
    // 0xC0000000 – 0xDFFFFFFF - Full 512mb Ram Memory Region

    private Xbox360Emulator? Xbox360emulator
    {
        get => (Xbox360Emulator?)emulatorClass;
        set => emulatorClass = value;
    }

    public Xbox360()
#if LIVESPLIT
        : this(true) { }

    public Xbox360(bool generateCode)
        : base(generateCode)
#else
        : base()
#endif
    {
        Log.Info("  => Xbox360 Helper started");
    }

    internal override string[] ProcessNames { get; } =
    [
        "xenia.exe",
        "xenia_canary.exe",
    ];

    public override bool TryGetRealAddress(ulong address, out IntPtr realAddress)
    {
        if (Xbox360emulator is null)
        {
            realAddress = default;
            return false;
        }

        realAddress = (IntPtr)((ulong)Xbox360emulator.RamBase + address);
        return true;
    }

    internal override Emulator? AttachEmuClass()
    {
        if (emulatorProcess is null)
            return null;

        return emulatorProcess.ProcessName switch
        {
            "xenia.exe" or "xenia_canary.exe" => new Xenia(),
            _ => null,
        };
    }

    public override bool TryRead<T>(out T value, ulong address, params int[] offsets)
    {
        if (emulatorProcess is null || Xbox360emulator is null || !ResolvePath(out IntPtr realAddress, address, offsets))
        {
            value = default;
            return false;
        }

        if (!emulatorProcess.Read(realAddress, out value))
            return false;

        value = value.FromEndian(Xbox360emulator.Endianness);
        return true;
    }

    protected override bool ResolvePath(out IntPtr finalAddress, ulong baseAddress, params int[] offsets)
    {
        // Check if the emulator process is valid and retrieve the real address for the base address
        if (emulatorProcess is null || Xbox360emulator is null || !TryGetRealAddress(baseAddress, out finalAddress))
        {
            finalAddress = default;
            return false;
        }

        foreach (int offset in offsets)
        {
            if (!emulatorProcess.Read(finalAddress, out uint tempAddress)
                || !TryGetRealAddress((ulong)(tempAddress.FromEndian(Xbox360emulator.Endianness) + offset), out finalAddress))
                return false;
        }

        return true;
    }

    public override unsafe bool TryReadArray<T>(out T[] value, uint size, ulong address, params int[] offsets)
    {
        if (emulatorProcess is null || Xbox360emulator is null || !ResolvePath(out IntPtr realAddress, address, offsets))
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

            Endian.FromEndian(buffer.Span, Xbox360emulator.Endianness);
            value = buffer.Span.ToArray();
        }

        return true;
    }
}