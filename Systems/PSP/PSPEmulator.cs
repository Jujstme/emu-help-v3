using EmuHelp.HelperBase;
using System;

namespace EmuHelp.Systems.PSP;

public abstract class PSPEmulator : Emulator
{
    public IntPtr RamBase { get; protected set; }
}