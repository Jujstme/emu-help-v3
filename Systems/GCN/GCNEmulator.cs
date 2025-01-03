﻿using Helper.HelperBase;
using JHelper.Common.MemoryUtils;
using System;

namespace Helper.GCN;

public abstract class GCNEmulator : Emulator
{
    public IntPtr MEM1 { get; protected set; }
    public Endianness Endianness { get; protected set; }
}