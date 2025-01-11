﻿using EmuHelp.HelperBase;
using EmuHelp.Systems.GCN;
using EmuHelp.Systems.GCN.Emulators;
using System;
using System.Runtime.InteropServices;
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
        value = default;

        if (emulatorProcess is null || Gcnemulator is null || !ResolvePath(out IntPtr realAddress, address, offsets))
            return false;

        int t_size;
        unsafe
        {
            t_size = sizeof(T);
        }

        using (ArrayRental<byte> buffer = t_size <= 1024 ? new(stackalloc byte[t_size]) : new(t_size))
        {
            Span<byte> span = buffer.Span;

            if (!emulatorProcess.ReadArray(realAddress, span))
                return false;

            if (Gcnemulator.Endianness == Endianness.Big)
                span.Reverse();

            unsafe
            {
                fixed (byte* ptr = span)
                    value = *(T*)ptr;
            }
        }
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

    public override bool TryReadArray<T>(out T[] value, uint size, ulong address, params int[] offsets)
    {
        if (emulatorProcess is null || Gcnemulator is null || !ResolvePath(out IntPtr realAddress, address, offsets))
        {
            value = new T[size];
            return false;
        }

        int t_size;
        unsafe
        {
            t_size = sizeof(T) * (int)size;
        }

        using (ArrayRental<byte> buffer = t_size <= 1024 ? new(stackalloc byte[t_size]) : new(t_size))
        {
            if (!emulatorProcess.ReadArray(realAddress, buffer.Span))
            {
                value = new T[t_size];
                return false;
            }

            if (Gcnemulator.Endianness == Endianness.Big)
            {
                int s;
                unsafe
                {
                    s = sizeof(T);
                }

                for (int i = 0; i < size; i++)
                    buffer.Span[(s * i)..(s * (i + 1))].Reverse();
            }

            Span<T> newBuf = MemoryMarshal.Cast<byte, T>(buffer.Span);
            value = newBuf[..(int)size].ToArray();
        }

        return true;
    }
}